using Microsoft.AspNetCore.Mvc;

namespace WebApplication5.Controllers
{
    public class UserManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
