using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(int? schoolId)
        {
            if (schoolId == null)
            {
                return RedirectToAction("Index", new { schoolId = 1 });
            }

            ViewBag.CurrentSchoolId = schoolId;
            return View();
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
}
