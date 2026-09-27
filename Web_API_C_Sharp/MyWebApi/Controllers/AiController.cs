using Microsoft.AspNetCore.Mvc;

namespace MyWebApi.Controllers
{
    public class AiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
