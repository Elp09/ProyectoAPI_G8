using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace proyecto.Web.Controllers;

[Authorize]
public class ApisController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ApisController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateClientWithToken();
        var response = await client.GetAsync("api/sources");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Error al cargar las fuentes.";
            return View(new List<ApiSource>());
        }

        var sources = (JsonSerializer.Deserialize<List<ApiSource>>(
            await response.Content.ReadAsStringAsync(), _json) ?? new())
            .Where(s => s.Name != "Subido localmente")
            .ToList();

        var secretsMap = new Dictionary<int, string>();
        if (User.IsInRole("Admin"))
        {
            var secretsResponse = await client.GetAsync("api/secrets");
            if (secretsResponse.IsSuccessStatusCode)
            {
                var secrets = JsonSerializer.Deserialize<List<SecretDto>>(
                    await secretsResponse.Content.ReadAsStringAsync(), _json) ?? new();
                foreach (var s in secrets)
                    if (s.SourceId.HasValue)
                        secretsMap[s.SourceId.Value] = s.KeyValue;
            }
        }
        ViewBag.SecretsMap = secretsMap;

        return View(sources);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string name, string url, string? description,
        string? secret, string? authType, string? endpoint)
    {
        var client = CreateClientWithToken();

        var sourcePayload = new
        {
            name,
            url,
            description,
            componentType = "api",
            requiresSecret = !string.IsNullOrWhiteSpace(secret),
            authType = string.IsNullOrWhiteSpace(secret) ? "none" : (authType ?? "query"),
            endpoint
        };

        var response = await client.PostAsync("api/sources",
            ToJson(sourcePayload));

        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(secret))
        {
            var created = JsonSerializer.Deserialize<ApiSource>(
                await response.Content.ReadAsStringAsync(), _json);

            if (created != null)
            {
                var secretPayload = new { sourceId = created.Id, keyName = "api-secret", keyValue = secret };
                await client.PostAsync("api/secrets", ToJson(secretPayload));
            }
        }

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Fuente agregada correctamente.";
        else
        {
            var body = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error {(int)response.StatusCode}: {body}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, string url, string? description,
        string? secret, string? authType, string? endpoint)
    {
        var client = CreateClientWithToken();

        var sourcePayload = new
        {
            id,
            name,
            url,
            description,
            componentType = "api",
            requiresSecret = !string.IsNullOrWhiteSpace(secret),
            authType = string.IsNullOrWhiteSpace(secret) ? "none" : (authType ?? "query"),
            endpoint
        };

        var response = await client.PutAsync($"api/sources/{id}", ToJson(sourcePayload));

        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(secret))
        {
            // Delete existing secrets for this source then add new one
            var existingSecrets = await client.GetAsync($"api/secrets/source/{id}");
            if (existingSecrets.IsSuccessStatusCode)
            {
                var secrets = JsonSerializer.Deserialize<List<SecretDto>>(
                    await existingSecrets.Content.ReadAsStringAsync(), _json);
                if (secrets != null)
                    foreach (var s in secrets)
                        await client.DeleteAsync($"api/secrets/{s.Id}");
            }
            var secretPayload = new { sourceId = id, keyName = "api-secret", keyValue = secret };
            await client.PostAsync("api/secrets", ToJson(secretPayload));
        }

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Fuente actualizada correctamente.";
        else
        {
            var body = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error {(int)response.StatusCode}: {body}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateClientWithToken();
        await client.DeleteAsync($"api/sources/{id}");
        TempData["Success"] = "Fuente eliminada.";
        return RedirectToAction(nameof(Index));
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
