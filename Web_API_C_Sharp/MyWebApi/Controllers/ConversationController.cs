using Microsoft.AspNetCore.Mvc;

namespace MyWebApi.Controllers
{
    public class ConversationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
