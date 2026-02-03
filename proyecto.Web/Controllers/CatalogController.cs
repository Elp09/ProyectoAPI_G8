using Microsoft.AspNetCore.Mvc;

namespace proyecto.Web.Controllers
{
    public class CatalogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
