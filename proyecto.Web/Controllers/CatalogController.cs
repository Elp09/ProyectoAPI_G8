using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using proyecto.Web.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace proyecto.Web.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly InMemoryStore _store;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleTextToSpeechService _ttsService;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CatalogController(
        InMemoryStore store,
        IHttpClientFactory httpClientFactory,
        GoogleTextToSpeechService ttsService)
    {
        _store = store;
        _httpClientFactory = httpClientFactory;
        _ttsService = ttsService;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateClientWithToken();

        // Cargar fuentes para el dropdown
        var sourcesResponse = await client.GetAsync("api/sources");
        var sources = sourcesResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<ApiSource>>(
                await sourcesResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<ApiSource>();

        // Cargar ítems guardados en BD
        var itemsResponse = await client.GetAsync("api/sourceitems");
        var dbItems = itemsResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<SavedItemDto>>(
                await itemsResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<SavedItemDto>();

        ViewBag.Sources = sources;
        ViewBag.DbItems = dbItems;

        // Si no hay ítems temporales ni guardados, mostrar ítems de las fuentes (requisito del PDF)
        if (!_store.Items.Any() && !dbItems.Any() && sources.Any())
            TempData["Info"] = "No hay ítems guardados. Selecciona una fuente y haz clic en Consultar.";

        return View(_store.Items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingest(int sourceId, string? endpointOverride)
    {
        var client = CreateClientWithToken();

        // Obtener la fuente
        var sourceResponse = await client.GetAsync($"api/sources/{sourceId}");
        if (!sourceResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Fuente no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var source = JsonSerializer.Deserialize<ApiSource>(
            await sourceResponse.Content.ReadAsStringAsync(), _json)!;

        // Construir URL final
        var endpoint = string.IsNullOrWhiteSpace(endpointOverride) ? source.Endpoint : endpointOverride;
        var requestUrl = string.IsNullOrWhiteSpace(endpoint)
            ? source.Url
            : $"{source.Url.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        // Buscar secreto si aplica
        string? secret = null;
        if (source.RequiresSecret)
        {
            var secretResponse = await client.GetAsync($"api/secrets/source/{sourceId}");
            if (secretResponse.IsSuccessStatusCode)
            {
                var secrets = JsonSerializer.Deserialize<List<SecretDto>>(
                    await secretResponse.Content.ReadAsStringAsync(), _json);
                secret = secrets?.FirstOrDefault()?.KeyValue;
            }
        }

        // Llamar a la fuente externa
        var externalClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        if (!string.IsNullOrWhiteSpace(secret))
        {
            request.Headers.TryAddWithoutValidation("Authorization",
                source.AuthType switch
                {
                    "bearer" => $"Bearer {secret}",
                    "apikey" => $"ApiKey {secret}",
                    "basic"  => $"Basic {secret}",
                    _        => secret
                });
        }

        var externalResponse = await externalClient.SendAsync(request);
        var rawJson = await externalResponse.Content.ReadAsStringAsync();

        // Normalizar via API
        var normalizePayload = new { rawJson };
        var normalizeResponse = await client.PostAsync(
            $"api/sourceitems/normalize/{sourceId}",
            ToJson(normalizePayload));

        if (!normalizeResponse.IsSuccessStatusCode)
        {
            var err = await normalizeResponse.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error al normalizar: {err}";
            return RedirectToAction(nameof(Index));
        }

        var normalizedJsons = JsonSerializer.Deserialize<List<string>>(
            await normalizeResponse.Content.ReadAsStringAsync(), _json) ?? new();

        foreach (var normalizedJson in normalizedJsons)
        {
            _store.Items.Add(new IngestedItem
            {
                SourceId      = sourceId,
                SourceName    = source.Name,
                Endpoint      = endpoint,
                IsLocalUpload = false,
                FetchedAt     = DateTime.Now,
                Json          = normalizedJson
            });
        }

        TempData["Success"] = $"{normalizedJsons.Count} ítem(s) ingestado(s). Guárdalos en la BD cuando estés listo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, int sourceId)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Archivo inválido.";
            return RedirectToAction(nameof(Index));
        }

        var client = CreateClientWithToken();

        var sourceResponse = await client.GetAsync($"api/sources/{sourceId}");
        if (!sourceResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Fuente no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var source = JsonSerializer.Deserialize<ApiSource>(
            await sourceResponse.Content.ReadAsStringAsync(), _json)!;

        using var reader = new StreamReader(file.OpenReadStream());
        var rawJson = await reader.ReadToEndAsync();

        // Normalizar via API (también valida el esquema si ya está normalizado)
        var normalizePayload = new { rawJson };
        var normalizeResponse = await client.PostAsync(
            $"api/sourceitems/normalize/{sourceId}",
            ToJson(normalizePayload));

        if (!normalizeResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Error al procesar el archivo.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedJsons = JsonSerializer.Deserialize<List<string>>(
            await normalizeResponse.Content.ReadAsStringAsync(), _json) ?? new();

        foreach (var normalizedJson in normalizedJsons)
        {
            _store.Items.Add(new IngestedItem
            {
                SourceId      = sourceId,
                SourceName    = source.Name,
                Endpoint      = file.FileName,
                IsLocalUpload = true,
                FetchedAt     = DateTime.Now,
                Json          = normalizedJson
            });
        }

        TempData["Success"] = $"{normalizedJsons.Count} ítem(s) cargado(s) desde archivo.";
        return RedirectToAction(nameof(Index));
    }

    // Guardar un ítem temporal en la BD
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Guid id)
    {
        var item = _store.Items.FirstOrDefault(i => i.Id == id);
        if (item == null)
        {
            TempData["Error"] = "Ítem no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var client = CreateClientWithToken();
        var payload = new { normalizedJson = item.Json, endpoint = item.Endpoint, isLocalUpload = item.IsLocalUpload };

        var response = await client.PostAsync(
            $"api/sourceitems/save/{item.SourceId}",
            ToJson(payload));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error al guardar: {err}";
            return RedirectToAction(nameof(Index));
        }

        _store.Items.Remove(item);
        TempData["Success"] = "Ítem guardado en la base de datos.";
        return RedirectToAction(nameof(Index));
    }

    // Descargar ítem temporal
    public IActionResult Download(Guid id)
    {
        var item = _store.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        var bytes = Encoding.UTF8.GetBytes(item.Json);
        var fileName = $"{item.SourceName}-{DateTime.Now:yyyyMMddHHmmss}.json";
        return File(bytes, "application/json", fileName);
    }

    // Descargar ítem guardado en BD
    public async Task<IActionResult> DownloadSaved(int id)
    {
        var client = CreateClientWithToken();
        var response = await client.GetAsync($"api/sourceitems/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();

        var item = JsonSerializer.Deserialize<SavedItemDto>(
            await response.Content.ReadAsStringAsync(), _json);
        if (item == null) return NotFound();

        var bytes = Encoding.UTF8.GetBytes(item.Json);
        return File(bytes, "application/json", $"item-{id}-{DateTime.Now:yyyyMMddHHmmss}.json");
    }

    // Eliminar ítem guardado en BD (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSaved(int id)
    {
        var client = CreateClientWithToken();
        await client.DeleteAsync($"api/sourceitems/{id}");
        TempData["Success"] = "Ítem eliminado de la base de datos.";
        return RedirectToAction(nameof(Index));
    }

    // TTS
    [HttpPost]
    public async Task<IActionResult> Speak([FromBody] SpeakRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("No hay texto para leer.");

        try
        {
            var audioBytes = await _ttsService.GenerateSpeechAsync(
                request.Text,
                string.IsNullOrWhiteSpace(request.LanguageCode) ? "es-ES" : request.LanguageCode);
            return File(audioBytes, "audio/mpeg");
        }
        catch (Exception ex)
        {
            return BadRequest("Error generando audio: " + ex.Message);
        }
    }

    private HttpClient CreateClientWithToken()
    {
        var client = _httpClientFactory.CreateClient("Api");
        var token = HttpContext.Session.GetString("JWToken");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static StringContent ToJson(object obj)
        => new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}

public class SpeakRequest
{
    public string Text { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "es-ES";
}

public class SecretDto
{
    public int Id { get; set; }
    public int? SourceId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string KeyValue { get; set; } = string.Empty;
}
