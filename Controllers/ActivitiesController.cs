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

            // Bring all activities into memory
            var allActivities = await _context.Activity.ToListAsync();

            // Filter by current school
            var assignedActivities = allActivities
                .Where(a => a.SchoolId == schoolId)
                .ToList();

            var pagedActivities = assignedActivities.ToPagedList(pageNumber, pageSize);
            return View(pagedActivities);
        }

        public async Task<IActionResult> Create()
        {
            var schools = await _schoolRoleService.GetAllSchoolsAsync();
            ViewBag.Schools = new SelectList(schools, "Id", "Name");
            return View();
        }

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

            // Repopulate list on failed validation
            var schools = await _schoolRoleService.GetAllSchoolsAsync();
            ViewBag.SchoolList = schools;
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
