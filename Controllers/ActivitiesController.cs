using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

            // Get the role IDs for ActivityAdmin and ActivityMember
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
                .Where(ur => ur.ActivityId != null && ur.Activity != null)
                .ToListAsync();

            var activityVMs = activities.Select(activity =>
            {
                var relatedRoles = userRoles.Where(ur => ur.ActivityId == activity.Id);

                return new ActivitiesIndexViewModel
                {
                    Id = activity.Id,
                    Name = activity.Name,
                    Admins = relatedRoles
                        .Where(ur => ur.RoleId == activityAdminRoleId)
                        .Select(ur => $"{ur.User.FirstName} {ur.User.LastName}")
                        .ToList(),

                    Members = relatedRoles
                        .Where(ur => ur.RoleId == activityMemberRoleId)
                        .Select(ur => $"{ur.User.FirstName} {ur.User.LastName}")
                        .ToList()
                };
            });

            var pagedList = activityVMs.ToPagedList(pageNumber, pageSize);
            return View(pagedList);
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var activity = await _context.Activity.FindAsync(id);
            if (activity == null)
                return NotFound();

            return View(activity);
        }

        // POST: Activities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Activity activity)
        {
            if (id != activity.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(activity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivityExists(activity.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(activity);
        }

        // GET: Activities/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var activity = await _context.Activity.FirstOrDefaultAsync(m => m.Id == id);
            if (activity == null)
                return NotFound();

            return View(activity);
        }

        // POST: Activities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity = await _context.Activity.FindAsync(id);
            if (activity != null)
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
