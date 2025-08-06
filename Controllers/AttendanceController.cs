using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Controllers
{
    [Route("Activities/{activityId}/Attendance")]
    [Authorize(Policy = "AttendanceAccess")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int activityId, DateTime? startDate, DateTime? endDate)
        {
            var activity = await _context.Activity.FindAsync(activityId);
            if (activity == null) return NotFound();

            var allDates = await _context.AttendanceRecords
                .Where(a => a.ActivityId == activityId)
                .Select(a => a.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            // Default range
            var filteredDates = allDates
                .Where(d => (!startDate.HasValue || d >= startDate.Value.Date) &&
                            (!endDate.HasValue || d <= endDate.Value.Date))
                .ToList();

            var members = await _context.UserRoles
                .Where(ur => ur.ActivityId == activityId)
                .Join(_context.Users,
                    ur => ur.UserId,
                    u => u.Id,
                    (ur, u) => new { ur.RoleId, ur.UserId, u.UserName })
                .Where(x => _context.Roles.Any(r => r.Id == x.RoleId && r.Name == "ActivityMember"))
                .Select(x => new { x.UserId, x.UserName })
                .ToListAsync();

            var records = await _context.AttendanceRecords
                .Where(ar => ar.ActivityId == activityId)
                .ToListAsync();

            var viewModel = new AttendanceReportViewModel
            {
                ActivityName = activity.Name,
                Dates = filteredDates,
                Members = members.Select(member => new MemberAttendanceRow
                {
                    Name = member.UserName,
                    AttendancePerDate = filteredDates.Select(date =>
                        records.Any(r => r.UserId == member.UserId && r.Date.Date == date.Date && r.IsPresent)).ToList()
                }).ToList()
            };

            ViewBag.ActivityId = activityId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(viewModel);
        }

        [HttpGet("Edit")]
        public async Task<IActionResult> Edit(int activityId, DateTime date)
        {
            var activity = await _context.Activity.FindAsync(activityId);
            if (activity == null) return NotFound();

            var userIds = await _context.UserRoles
                .Where(ur => ur.ActivityId == activityId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "ActivityMember")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var attendanceRecords = await _context.AttendanceRecords
                .Where(ar => ar.ActivityId == activityId && ar.Date.Date == date.Date)
                .ToListAsync();

            var model = new EditAttendanceViewModel
            {
                ActivityId = activityId,
                Date = date,
                Members = users.Select(u => new MemberAttendanceCheckbox
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    IsPresent = attendanceRecords.Any(ar => ar.UserId == u.Id && ar.IsPresent)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAttendanceViewModel model)
        {
            var existingRecords = await _context.AttendanceRecords
                .Where(ar => ar.ActivityId == model.ActivityId && ar.Date.Date == model.Date.Date)
                .ToListAsync();

            foreach (var member in model.Members)
            {
                var record = existingRecords.FirstOrDefault(r => r.UserId == member.UserId);
                if (record != null)
                {
                    record.IsPresent = member.IsPresent;
                }
                else
                {
                    // Create a new record if it doesn't exist
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        UserId = member.UserId,
                        ActivityId = model.ActivityId,
                        Date = model.Date,
                        IsPresent = member.IsPresent
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { activityId = model.ActivityId });
        }
    }
}
