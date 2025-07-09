using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models;
using WebApplication5.Models.Interfaces;
using WebApplication5.Models.ViewModels;
using X.PagedList;
using X.PagedList.Mvc.Core;
using X.PagedList.Extensions;

namespace WebApplication5.Controllers
{
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentSchoolService _currentSchoolService;

        public ActivitiesController(ApplicationDbContext context, ICurrentSchoolService currentSchoolService)
        {
            _context = context;
            _currentSchoolService = currentSchoolService;
        }

        // GET: Activities
        public async Task<IActionResult> Index(int? assignedPage, int? unassignedPage)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            int pageSize = 10;

            var allUserRoles = await _context.UserRoles
                .Include(ur => ur.Activity)
                .Where(ur => ur.ActivityId != null)
                .ToListAsync();

            var assignedActivityIds = allUserRoles
                .Where(ur => ur.SchoolId == schoolId)
                .Select(ur => ur.ActivityId)
                .Distinct()
                .ToList();

            var assignedActivitiesList = await _context.Activity
                .Where(a => assignedActivityIds.Contains(a.Id))
                .ToListAsync();

            // 👇 Fix: pull all activities first, then filter in memory
            var allActivities = await _context.Activity.ToListAsync();

            var unassignedActivitiesList = allActivities
                .Where(a => !allUserRoles.Any(ur => ur.ActivityId == a.Id))
                .ToList();

            var assignedActivities = assignedActivitiesList.ToPagedList(assignedPage ?? 1, pageSize);
            var unassignedActivities = unassignedActivitiesList.ToPagedList(unassignedPage ?? 1, pageSize);

            var viewModel = new ActivitiesIndexViewModel
            {
                AssignedActivities = assignedActivities,
                UnassignedActivities = unassignedActivities
            };

            return View(viewModel);
        }


        // GET: Activities/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (ModelState.IsValid)
            {
                _context.Add(activity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(activity);
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
