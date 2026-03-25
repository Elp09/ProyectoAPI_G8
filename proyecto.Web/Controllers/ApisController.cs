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

        var sources = JsonSerializer.Deserialize<List<ApiSource>>(
            await response.Content.ReadAsStringAsync(), _json) ?? new();

        return View(sources);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string name, string url, string? description,
        string authType, string? secret, string? endpoint)
    {
        var client = CreateClientWithToken();

        var sourcePayload = new
        {
            name,
            url,
            description,
            componentType = "api",
            requiresSecret = !string.IsNullOrWhiteSpace(secret),
            authType = authType ?? "none",
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
