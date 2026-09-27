using Microsoft.AspNetCore.Mvc;

namespace MyWebApi.Controllers
{
    public class UsageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
