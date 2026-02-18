using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;

namespace proyecto.Web.Controllers;

public class ApisController : Controller
{
    private readonly InMemoryStore _store;

    public ApisController(InMemoryStore store)
    {
        _store = store;
    }

    public IActionResult Index()
    {
        return View(_store.Sources);
    }

    [HttpPost]
    public IActionResult Add(string name, string url, string authType, string? secret, string? endpoint)
    {
        _store.Sources.Add(new ApiSource
        {
            Name = name,
            Url = url,
            AuthType = authType,
            Secret = secret,
            Endpoint = endpoint
        });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(Guid id)
    {
        var source = _store.Sources.FirstOrDefault(s => s.Id == id);
        if (source != null)
            _store.Sources.Remove(source);

        return RedirectToAction(nameof(Index));
    }
}
