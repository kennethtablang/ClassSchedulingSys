using Microsoft.AspNetCore.Mvc;

namespace ClassSchedulingSys.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
