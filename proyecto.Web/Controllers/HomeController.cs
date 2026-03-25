using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace proyecto.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateClientWithToken();

        // Cargar fuentes
        var sourcesResponse = await client.GetAsync("api/sources");
        var sources = sourcesResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<ApiSource>>(
                await sourcesResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<ApiSource>();

        // Cargar ítems guardados
        var itemsResponse = await client.GetAsync("api/sourceitems");
        var items = itemsResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<SavedItemDto>>(
                await itemsResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<SavedItemDto>();

        ViewBag.SourcesCount = sources.Count;
        ViewBag.Sources      = sources.TakeLast(3).Reverse().ToList();
        ViewBag.ItemsCount   = items.Count;
        ViewBag.RecentItems  = items.Take(3).ToList();
        ViewBag.LastIngest   = items.Any() ? items.Max(i => i.CreatedAt) : (DateTime?)null;

        return View();
    }

    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private HttpClient CreateClientWithToken()
    {
        var client = _httpClientFactory.CreateClient("Api");
        var token = HttpContext.Session.GetString("JWToken");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
