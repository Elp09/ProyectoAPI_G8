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

        var sourcesResponse = await client.GetAsync("api/sources");
        var sources = sourcesResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<ApiSource>>(
                await sourcesResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<ApiSource>();

        var itemsResponse = await client.GetAsync("api/sourceitems");
        var dbItems = itemsResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<SavedItemDto>>(
                await itemsResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<SavedItemDto>();

        ViewBag.Sources = sources;
        ViewBag.DbItems = dbItems;

        if (!_store.Items.Any() && !dbItems.Any() && sources.Any())
            TempData["Info"] = "No hay ítems guardados. Selecciona una fuente y haz clic en Consultar.";

        return View(_store.Items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingest(int sourceId, string? endpointOverride)
    {
        var client = CreateClientWithToken();

        var sourceResponse = await client.GetAsync($"api/sources/{sourceId}");
        if (!sourceResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Fuente no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var source = JsonSerializer.Deserialize<ApiSource>(
            await sourceResponse.Content.ReadAsStringAsync(), _json)!;

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

        if (!string.IsNullOrWhiteSpace(secret))
        {
            if (source.AuthType == "query")
                requestUrl = requestUrl.Replace("{secret}", Uri.EscapeDataString(secret));
            else
                requestUrl = requestUrl.Replace("{secret}", Uri.EscapeDataString(secret));
        }

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", "ProyectoAPI_G8/1.0");

        if (!string.IsNullOrWhiteSpace(secret) && source.AuthType == "header")
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {secret}");

        HttpResponseMessage externalResponse;
        try { externalResponse = await externalClient.SendAsync(request); }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al contactar la fuente: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }

        var rawJson = await externalResponse.Content.ReadAsStringAsync();

        var items = ExtractItems(rawJson);
        foreach (var itemJson in items)
        {
            _store.Items.Add(new IngestedItem
            {
                SourceId      = sourceId,
                SourceName    = source.Name,
                Endpoint      = endpoint,
                IsLocalUpload = false,
                FetchedAt     = DateTime.Now,
                Json          = itemJson
            });
        }

        TempData["Success"] = items.Count == 1
            ? "Ítem ingestado. Guárdalo en la BD cuando estés listo."
            : $"{items.Count} ítems ingestados. Guárdalos en la BD cuando estés listo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? uploadSourceName)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Archivo inválido.";
            return RedirectToAction(nameof(Index));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var uploadedJson = await reader.ReadToEndAsync();

        var jsonToStore = IsIngestDocument(uploadedJson)
            ? ExtractOriginal(uploadedJson) ?? uploadedJson
            : uploadedJson;

        _store.Items.Add(new IngestedItem
        {
            SourceId      = 0,
            SourceName    = string.IsNullOrWhiteSpace(uploadSourceName) ? "Subida manual" : uploadSourceName,
            Endpoint      = file.FileName,
            IsLocalUpload = true,
            FetchedAt     = DateTime.Now,
            Json          = jsonToStore
        });

        TempData["Success"] = "Archivo cargado. Guárdalo en la BD cuando estés listo.";
        return RedirectToAction(nameof(Index));
    }

    // Guardar ítem temporal en BD (raw JSON directo)
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

        if (item.SourceId == 0)
        {
            TempData["Error"] = "Este ítem fue subido sin fuente. Elimínalo y súbelo de nuevo asociándolo a una fuente existente.";
            return RedirectToAction(nameof(Index));
        }

        var client = CreateClientWithToken();
        var payload = new { rawJson = item.Json, endpoint = item.Endpoint, isLocalUpload = item.IsLocalUpload };

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearMemory()
    {
        _store.Items.Clear();
        TempData["Success"] = "Memoria limpiada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteTemp(Guid id)
    {
        var item = _store.Items.FirstOrDefault(i => i.Id == id);
        if (item != null) _store.Items.Remove(item);
        return RedirectToAction(nameof(Index));
    }

    // Descargar ítem temporal — normaliza al exportar
    public async Task<IActionResult> Download(Guid id)
    {
        var item = _store.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        var client = CreateClientWithToken();
        var normalizedJson = await NormalizeForExport(client, item.Json, item.SourceId);

        var fileName = $"{item.SourceName}-{DateTime.Now:yyyyMMddHHmmss}.json";
        return File(Encoding.UTF8.GetBytes(normalizedJson), "application/json", fileName);
    }

    // Descargar ítem guardado en BD — normaliza al exportar
    public async Task<IActionResult> DownloadSaved(int id)
    {
        var client = CreateClientWithToken();
        var response = await client.GetAsync($"api/sourceitems/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();

        var item = JsonSerializer.Deserialize<SavedItemDto>(
            await response.Content.ReadAsStringAsync(), _json);
        if (item == null) return NotFound();

        var normalizedJson = await NormalizeForExport(client, item.Json, item.SourceId);
        return File(Encoding.UTF8.GetBytes(normalizedJson), "application/json", $"item-{id}-{DateTime.Now:yyyyMMddHHmmss}.json");
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

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Llama a api/sourceitems/normalize y devuelve el JSON normalizado listo para descargar.</summary>
    private async Task<string> NormalizeForExport(HttpClient client, string rawJson, int sourceId)
    {
        var normalizeResponse = await client.PostAsync(
            $"api/sourceitems/normalize/{sourceId}",
            ToJson(new { rawJson }));

        if (!normalizeResponse.IsSuccessStatusCode)
            return rawJson; // fallback: devolver crudo si falla

        var docs = JsonSerializer.Deserialize<List<string>>(
            await normalizeResponse.Content.ReadAsStringAsync(), _json) ?? new();

        // Si hay un solo elemento devolver ese, si hay varios devolver array JSON
        if (docs.Count == 1) return docs[0];
        if (docs.Count > 1)  return "[" + string.Join(",", docs) + "]";
        return rawJson;
    }

    private static List<string> ExtractItems(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                return root.EnumerateArray().Select(e => e.GetRawText()).ToList();

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var elements = prop.Value.EnumerateArray().Select(e => e.GetRawText()).ToList();
                        if (elements.Count > 0) return elements;
                    }
                }
            }
        }
        catch { }

        return new List<string> { json };
    }

    private static bool IsIngestDocument(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("schemaVersion", out var sv)
                && sv.GetString() == "edu.univ.ingest.v1";
        }
        catch { return false; }
    }

    private static string? ExtractOriginal(string ingestJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(ingestJson);
            var root = doc.RootElement;

            // Busca raw.data.original de forma case-insensitive
            foreach (var p1 in root.EnumerateObject())
            {
                if (!p1.Name.Equals("raw", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var p2 in p1.Value.EnumerateObject())
                {
                    if (!p2.Name.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var p3 in p2.Value.EnumerateObject())
                    {
                        if (!p3.Name.Equals("original", StringComparison.OrdinalIgnoreCase)) continue;
                        return p3.Value.GetRawText();
                    }
                }
            }
        }
        catch { }
        return null;
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
