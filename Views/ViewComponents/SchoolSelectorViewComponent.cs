using Microsoft.AspNetCore.Mvc;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Views.ViewComponents
{
    public class SchoolSelectorViewComponent : ViewComponent
    {
        private readonly ISchoolRoleService _schoolRoleService;

        public SchoolSelectorViewComponent(ISchoolRoleService schoolRoleService)
        {
            _schoolRoleService = schoolRoleService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? currentSchoolId)
        {
            var schools = await _schoolRoleService.GetAllSchoolsAsync();
            ViewBag.CurrentSchoolId = currentSchoolId ?? 1;
            return View(schools);
        }
    }
}
