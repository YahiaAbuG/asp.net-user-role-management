using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICurrentSchoolService _currentSchoolService;
        private readonly ISchoolRoleService _schoolRoleService;

        public ActivitiesController(ApplicationDbContext context,
            ICurrentSchoolService currentSchoolService, 
            ISchoolRoleService schoolRoleService, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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

        // GET: Activities/Manage/5
        [AuthorizeSchoolRole("Admin, Manager")]
        public async Task<IActionResult> Manage(int id)
        {
            var activity = await _context.Activity.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            int currentSchoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;

            var activityAdminRole = await _roleManager.FindByNameAsync("ActivityAdmin");
            var activityMemberRole = await _roleManager.FindByNameAsync("ActivityMember");

            var userRoles = await _context.UserRoles
                .Where(ur => ur.ActivityId == id)
                .ToListAsync();

            var usersInSchool = await _userManager.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.SchoolId == currentSchoolId))
                .ToListAsync();

            var viewModel = new ManageActivityUsersViewModel
            {
                Activity = activity,
                Users = usersInSchool.Select(u =>
                {
                    var roleAssignment = userRoles.FirstOrDefault(ur => ur.UserId == u.Id);
                    string currentRole = null;

                    if (roleAssignment != null)
                    {
                        var roleId = roleAssignment.RoleId;
                        if (roleId == activityAdminRole?.Id) currentRole = "ActivityAdmin";
                        else if (roleId == activityMemberRole?.Id) currentRole = "ActivityMember";
                    }

                    return new ActivityUserRoleAssignment
                    {
                        UserId = u.Id,
                        UserName = u.UserName,
                        FullName = $"{u.FirstName} {u.LastName}",
                        CurrentRole = currentRole
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Activities/Manage/5
        [AuthorizeSchoolRole("Admin, Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int id, List<ActivityUserRoleAssignment> users)
        {
            var activity = await _context.Activity.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            var adminRole = await _roleManager.FindByNameAsync("ActivityAdmin");
            var memberRole = await _roleManager.FindByNameAsync("ActivityMember");

            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;

            foreach (var userAssignment in users)
            {
                var existingRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userAssignment.UserId && ur.ActivityId == id);

                if (existingRole != null)
                {
                    // Update role or remove if set to "None"
                    if (userAssignment.CurrentRole == "ActivityAdmin")
                        existingRole.RoleId = adminRole.Id;
                    else if (userAssignment.CurrentRole == "ActivityMember")
                        existingRole.RoleId = memberRole.Id;
                    else
                        _context.UserRoles.Remove(existingRole);
                }
                else
                {
                    if (userAssignment.CurrentRole == "ActivityAdmin" || userAssignment.CurrentRole == "ActivityMember")
                    {
                        var roleId = userAssignment.CurrentRole == "ActivityAdmin" ? adminRole.Id : memberRole.Id;
                        _context.UserRoles.Add(new ApplicationUserRole
                        {
                            UserId = userAssignment.UserId,
                            ActivityId = id,
                            SchoolId = schoolId,
                            RoleId = roleId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
