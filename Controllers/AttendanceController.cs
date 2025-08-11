using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models.ViewModels;
using WebApplication5.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        // REPORT
        [HttpGet("")]
        public async Task<IActionResult> Index(int activityId, DateTime? startDate, DateTime? endDate)
        {
            var activity = await _context.Activity.FindAsync(activityId);
            if (activity == null) return NotFound();

            // sessions (filterable)
            var sessionsQuery = _context.AttendanceSessions
                .Where(s => s.ActivityId == activityId);

            if (startDate.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date.Date <= endDate.Value.Date);

            var sessions = await sessionsQuery
                .OrderBy(s => s.Date)
                .ToListAsync();

            var dates = sessions.Select(s => s.Date.Date).ToList();
            var sessionIds = sessions.Select(s => s.Id).ToList();

            // all attendance records for these sessions
            var records = await _context.AttendanceRecords
                .Where(r => sessionIds.Contains(r.AttendanceSessionId))
                .ToListAsync();

            // members for this activity
            var memberUserIds = await _context.UserRoles
                .Where(ur => ur.ActivityId == activityId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "ActivityMember")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            var members = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var viewModel = new AttendanceReportViewModel
            {
                ActivityName = activity.Name,
                Dates = dates,
                Members = members.Select(m => new MemberAttendanceRow
                {
                    Name = m.UserName,
                    AttendancePerDate = dates.Select(d =>
                    {
                        // find the session for that date (dates list & sessions are aligned)
                        var sessionId = sessions.First(s => s.Date.Date == d).Id;
                        return records.Any(r => r.AttendanceSessionId == sessionId && r.UserId == m.Id);
                    }).ToList()
                }).ToList()
            };

            ViewBag.ActivityId = activityId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(viewModel);
        }

        // TAKE - GET
        [HttpGet("Take", Name = "Attendance_Take")]
        public async Task<IActionResult> Take(int activityId, DateTime? date)
        {
            var activity = await _context.Activity.FindAsync(activityId);
            if (activity == null) return NotFound();
            if (date == null) return BadRequest();

            var selectedDate = date.Value.Date;

            // members
            var memberUserIds = await _context.UserRoles
                .Where(ur => ur.ActivityId == activityId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "ActivityMember")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .ToListAsync();

            // session for that day
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.ActivityId == activityId && s.Date.Date == selectedDate);

            if (session == null)
            {
                // optional: create it so page always works
                session = new AttendanceSession { ActivityId = activityId, Date = selectedDate };
                _context.AttendanceSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            var presentRecords = await _context.AttendanceRecords
                .Where(r => r.AttendanceSessionId == session.Id)
                .Select(r => r.UserId)
                .ToListAsync();

            var model = new EditAttendanceViewModel
            {
                ActivityId = activityId,
                Date = selectedDate,
                Members = users.Select(u => new MemberAttendanceCheckbox
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    IsPresent = presentRecords.Contains(u.Id)
                }).ToList()
            };

            ViewBag.ActivityId = activityId;
            ViewBag.ActivityName = activity.Name;
            return View(model);
        }

        // TAKE - POST
        [HttpPost("Take")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(EditAttendanceViewModel model)
        {
            var selectedDate = model.Date.Date;

            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.ActivityId == model.ActivityId && s.Date.Date == selectedDate);

            if (session == null) return BadRequest(ModelState);

            var existing = await _context.AttendanceRecords
                .Where(r => r.AttendanceSessionId == session.Id)
                .ToListAsync();

            var existingByUser = existing.ToDictionary(r => r.UserId, r => r);

            // presence by existence
            foreach (var m in model.Members ?? Enumerable.Empty<MemberAttendanceCheckbox>())
            {
                var hasRecord = existingByUser.TryGetValue(m.UserId, out var rec);

                if (m.IsPresent)
                {
                    if (!hasRecord)
                    {
                        _context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            AttendanceSessionId = session.Id,
                            UserId = m.UserId
                        });
                    }
                }
                else
                {
                    if (hasRecord)
                    {
                        _context.AttendanceRecords.Remove(rec);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // stay on same date
            return RedirectToAction("Take", new { activityId = model.ActivityId, date = selectedDate.ToString("yyyy-MM-dd") });
        }
    }
}
