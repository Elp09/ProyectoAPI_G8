using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace proyecto.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Landing()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index));

        return View();
    }

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

    public async Task<IActionResult> Catalog()
    {
        var client = CreateClientWithToken();

        var sourcesResponse = await client.GetAsync("api/sources");
        var sources = sourcesResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<ApiSource>>(
                await sourcesResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<ApiSource>();

        var itemsResponse = await client.GetAsync("api/sourceitems");
        var rawItems = itemsResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<List<SavedItemDto>>(
                await itemsResponse.Content.ReadAsStringAsync(), _json) ?? new()
            : new List<SavedItemDto>();

        var sourceMap = sources.ToDictionary(s => s.Id, s => s.Name);

        var cards = rawItems.Select(item => new CatalogCardVm
        {
            Id         = item.Id,
            SourceId   = item.SourceId,
            SourceName = sourceMap.TryGetValue(item.SourceId, out var name) ? name : $"Fuente #{item.SourceId}",
            CreatedAt  = item.CreatedAt,
            Fields     = ParseJsonFields(item.Json),
        }).ToList();

        ViewBag.Sources = sources;
        return View(cards);
    }

    private static List<CardField> ParseJsonFields(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var fields = new List<CardField>();
            FlattenElement(doc.RootElement, fields, new HashSet<string>());
            return fields;
        }
        catch
        {
            return new List<CardField> { new CardField { Label = "Contenido", Value = json } };
        }
    }

    // Recursively extracts every primitive from any JSON structure.
    // 'seen' deduplicates by label so IngestDocument wrapper fields don't
    // shadow the same field coming from raw.data.original.
    private static void FlattenElement(JsonElement el, List<CardField> fields, HashSet<string> seen)
    {
        if (el.ValueKind != JsonValueKind.Object) return;

        foreach (var prop in el.EnumerateObject())
        {
            var label = FormatLabel(prop.Name);

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    var str = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(str) && seen.Add(label))
                        fields.Add(new CardField { Label = label, Value = str });
                    break;

                case JsonValueKind.Number:
                    if (seen.Add(label))
                        fields.Add(new CardField { Label = label, Value = prop.Value.ToString() });
                    break;

                case JsonValueKind.True:
                    if (seen.Add(label))
                        fields.Add(new CardField { Label = label, Value = "Sí" });
                    break;

                case JsonValueKind.False:
                    if (seen.Add(label))
                        fields.Add(new CardField { Label = label, Value = "No" });
                    break;

                case JsonValueKind.Object:
                    FlattenElement(prop.Value, fields, seen);
                    break;

                case JsonValueKind.Array:
                    var parts = new List<string>();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                            parts.Add(item.GetString() ?? "");
                        else if (item.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                            parts.Add(item.ToString());
                        else if (item.ValueKind == JsonValueKind.Object)
                            FlattenElement(item, fields, seen);
                    }
                    if (parts.Count > 0 && seen.Add(label))
                        fields.Add(new CardField { Label = label, Value = string.Join(", ", parts) });
                    break;
            }
        }
    }

    private static string FormatLabel(string key)
    {
        var spaced = Regex.Replace(key, "([A-Z])", " $1");
        spaced = spaced.Replace("_", " ").Replace("-", " ");
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.Trim().ToLower());
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
