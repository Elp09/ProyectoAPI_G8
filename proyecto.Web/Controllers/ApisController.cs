using Microsoft.AspNetCore.Mvc;

namespace proyecto.Web.Controllers
{
    public class ApisController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
