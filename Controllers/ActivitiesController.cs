using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Attributes;
using WebApplication5.Data;
using WebApplication5.Models;
using WebApplication5.Models.Interfaces;
using WebApplication5.Models.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;

namespace WebApplication5.Controllers
{
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentSchoolService _currentSchoolService;
        private readonly ISchoolRoleService _schoolRoleService;

        public ActivitiesController(ApplicationDbContext context, ICurrentSchoolService currentSchoolService, ISchoolRoleService schoolRoleService)
        {
            _context = context;
            _currentSchoolService = currentSchoolService;
            _schoolRoleService = schoolRoleService;
        }

        // GET: Activities
        public async Task<IActionResult> Index(int? page)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Get role IDs
            var activityAdminRoleId = await _context.Roles
                .Where(r => r.Name == "ActivityAdmin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var activityMemberRoleId = await _context.Roles
                .Where(r => r.Name == "ActivityMember")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var activities = await _context.Activity
                .Where(a => a.SchoolId == schoolId)
                .ToListAsync();

            var userRoles = await _context.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.ActivityId != null)
                .ToListAsync();

            var viewModels = activities.Select(a => new ActivitiesIndexViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Admins = userRoles
                    .Where(ur => ur.ActivityId == a.Id && ur.RoleId == activityAdminRoleId)
                    .Select(ur => $"{ur.User.FirstName} {ur.User.LastName}")
                    .ToList(),
                Members = userRoles
                    .Where(ur => ur.ActivityId == a.Id && ur.RoleId == activityMemberRoleId)
                    .Select(ur => $"{ur.User.FirstName} {ur.User.LastName}")
                    .ToList()
            }).ToList();

            return View(viewModels.ToPagedList(pageNumber, pageSize));
        }


        // GET: Activities/Create
        public async Task<IActionResult> Create()
        {
            var schools = await _schoolRoleService.GetAllSchoolsAsync();
            ViewBag.Schools = new SelectList(schools, "Id", "Name");
            return View();
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateActivityViewModel model)
        {
            if (ModelState.IsValid)
            {
                var activity = new Activity
                {
                    Name = model.Name,
                    SchoolId = model.SchoolId
                };

                _context.Activity.Add(activity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var schools = await _schoolRoleService.GetAllSchoolsAsync();
            ViewBag.Schools = new SelectList(schools, "Id", "Name", model.SchoolId);
            return View(model);
        }


        // GET: Activities/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var activity = await _context.Activity.FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null)
            {
                return NotFound();
            }

            var viewModel = new EditActivityViewModel
            {
                Id = activity.Id,
                Name = activity.Name
            };

            return View(viewModel);
        }

        // POST: Activities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditActivityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var activity = await _context.Activity.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (activity == null)
            {
                return NotFound();
            }

            activity.Name = model.Name;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Activities/Delete/5
        [AuthorizeSchoolRole("Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _context.Activity.FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null)
            {
                ViewBag.ErrorMessage = $"Activity with ID = {id} cannot be found";
                return View("NotFound");
            }

            return View(activity);
        }

        // POST: Activities/Delete/5
        [AuthorizeSchoolRole("Admin,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity = await _context.Activity.FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null)
            {
                ViewBag.ErrorMessage = $"Activity with ID = {id} cannot be found";
                return RedirectToAction(nameof(Index));
            }

            _context.Activity.Remove(activity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ActivityExists(int id)
        {
            return _context.Activity.Any(e => e.Id == id);
        }
    }
}
