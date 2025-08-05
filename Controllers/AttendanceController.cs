using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Controllers
{
    [Route("Activities/{activityId}/Attendance")]
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

        [HttpGet("Date/{date}")]
        public async Task<IActionResult> ByDate(int activityId, DateTime date)
        {
            var records = await _context.AttendanceRecords
                .Include(a => a.User)
                .Where(a => a.ActivityId == activityId && a.Date.Date == date.Date)
                .ToListAsync();

            ViewBag.ActivityId = activityId;
            ViewBag.Date = date;
            return View(records);
        }

        [HttpGet("Range")]
        public IActionResult DateRange(int activityId)
        {
            ViewBag.ActivityId = activityId;
            return View();
        }

        [HttpPost("Range")]
        public async Task<IActionResult> DateRange(int activityId, DateTime startDate, DateTime endDate)
        {
            var grouped = await _context.AttendanceRecords
                .Include(a => a.User)
                .Where(a => a.ActivityId == activityId &&
                            a.Date.Date >= startDate.Date &&
                            a.Date.Date <= endDate.Date)
                .GroupBy(a => a.User)
                .ToListAsync();

            ViewBag.ActivityId = activityId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View("RangeResult", grouped);
        }
    }
}
