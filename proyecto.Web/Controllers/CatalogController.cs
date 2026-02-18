using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Text;
using System.Text.Json;

namespace proyecto.Web.Controllers;

public class CatalogController : Controller
{
    private readonly InMemoryStore _store;
    private readonly IHttpClientFactory _httpClientFactory;

    public CatalogController(InMemoryStore store, IHttpClientFactory httpClientFactory)
    {
        _store = store;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        ViewBag.Sources = _store.Sources;
        return View(_store.Items);
    }

    [HttpPost]
    public async Task<IActionResult> Ingest(Guid sourceId, string? endpointOverride)
    {
        var source = _store.Sources.FirstOrDefault(s => s.Id == sourceId);
        if (source == null)
            return RedirectToAction(nameof(Index));

        var endpoint = string.IsNullOrWhiteSpace(endpointOverride)
            ? source.Endpoint
            : endpointOverride;

        var requestUrl = string.IsNullOrWhiteSpace(endpoint)
            ? source.Url
            : $"{source.Url.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        if (!string.IsNullOrWhiteSpace(source.Secret))
        {
            request.Headers.TryAddWithoutValidation("Authorization",
                source.AuthType switch
                {
                    "bearer" => $"Bearer {source.Secret}",
                    "apikey" => $"ApiKey {source.Secret}",
                    "basic"  => $"Basic {source.Secret}",
                    _        => source.Secret
                });
        }

        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        var recordCount = 1;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                recordCount = doc.RootElement.GetArrayLength();
        }
        catch { }

        _store.Items.Add(new IngestedItem
        {
            SourceName = source.Name,
            Endpoint = string.IsNullOrWhiteSpace(endpoint) ? "/" : endpoint,
            FetchedAt = DateTime.Now,
            Json = json,
            RecordCount = recordCount
        });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return RedirectToAction(nameof(Index));

        using var reader = new StreamReader(file.OpenReadStream());
        var json = await reader.ReadToEndAsync();

        var recordCount = 1;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                recordCount = doc.RootElement.GetArrayLength();
        }
        catch { }

        _store.Items.Add(new IngestedItem
        {
            SourceName = file.FileName,
            Endpoint = file.FileName,
            IsLocalUpload = true,
            FetchedAt = DateTime.Now,
            Json = json,
            RecordCount = recordCount
        });

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Download(Guid id)
    {
        var item = _store.Items.FirstOrDefault(i => i.Id == id);
        if (item == null)
            return NotFound();

        var bytes = Encoding.UTF8.GetBytes(item.Json);
        var fileName = $"{item.SourceName}-{item.Endpoint.Trim('/').Replace('/', '-')}-{item.FetchedAt:yyyyMMddHHmmss}.json";
        return File(bytes, "application/json", fileName);
    }
}
