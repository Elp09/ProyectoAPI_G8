using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Diagnostics;

namespace proyecto.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly InMemoryStore _store;

    public HomeController(ILogger<HomeController> logger, InMemoryStore store)
    {
        _logger = logger;
        _store = store;
    }

    public IActionResult Index()
    {
        return View(_store);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
