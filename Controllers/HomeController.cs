using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication5.Models.ViewModels;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICurrentSchoolService _currentSchoolService;

        public HomeController(ILogger<HomeController> logger, ICurrentSchoolService currentSchoolService)
        {
            _logger = logger;
            _currentSchoolService = currentSchoolService;
        }

        public IActionResult Index(int? schoolId)
        {
            if (schoolId == null)
            {
                return RedirectToAction("Index", new { schoolId = 1 });
            }

            ViewBag.CurrentSchoolId = schoolId.Value;
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
