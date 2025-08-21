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

        [HttpGet("Sessions")]
        public async Task<IActionResult> Sessions(int activityId)
        {
            var activity = await _context.Activity.FindAsync(activityId);
            if (activity == null) return NotFound();

            var sessions = await _context.AttendanceSessions
                .Where(s => s.ActivityId == activityId)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            ViewBag.ActivityId = activityId;
            ViewBag.ActivityName = activity.Name;
            return View(sessions);
        }

        // POST: Activities/{activityId}/Attendance/Toggle/{sessionId}
        [HttpPost("Toggle/{sessionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int activityId, int sessionId, bool isOpen)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ActivityId == activityId);
            if (session == null) return NotFound();

            session.IsOpen = isOpen;
            await _context.SaveChangesAsync();

            return RedirectToAction("Sessions", new { activityId });
        }

        // CREATE SESSION (inline form on Sessions page)
        [HttpPost("Sessions/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSession(int activityId, DateTime date)
        {
            // normalize to date-only
            var day = date.Date;

            var exists = await _context.AttendanceSessions
                .AnyAsync(s => s.ActivityId == activityId && s.Date.Date == day);

            if (!exists)
            {
                _context.AttendanceSessions.Add(new AttendanceSession
                {
                    ActivityId = activityId,
                    Date = day
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Sessions), new { activityId });
        }

        // DELETE SESSION (button in Sessions table)
        [HttpPost("Sessions/{sessionId}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(int activityId, int sessionId)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ActivityId == activityId);
            if (session == null) return NotFound();

            _context.AttendanceSessions.Remove(session); // cascades to AttendanceRecords
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Sessions), new { activityId });
        }

        // TAKE ATTENDANCE (now by sessionId)
        [HttpGet("Sessions/{sessionId}/Take")]
        public async Task<IActionResult> Take(int activityId, int sessionId)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ActivityId == activityId);
            if (session == null) return NotFound();

            if (!session.IsOpen) return Forbid();

            // members of this activity
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

            var presentRecords = await _context.AttendanceRecords
                .Where(r => r.AttendanceSessionId == session.Id)
                .Select(r => r.UserId)
                .ToListAsync();

            var model = new EditAttendanceViewModel
            {
                ActivityId = activityId,
                AttendanceSessionId = session.Id,
                Date = session.Date,
                Members = users.Select(u => new MemberAttendanceCheckbox
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    IsPresent = presentRecords.Contains(u.Id)
                }).ToList()
            };

            ViewBag.ActivityId = activityId;
            ViewBag.ActivityName = (await _context.Activity.FindAsync(activityId))?.Name;
            ViewBag.SessionDate = session.Date;

            return View(model);
        }

        [HttpPost("Sessions/{sessionId}/Take")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(EditAttendanceViewModel model)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == model.AttendanceSessionId && s.ActivityId == model.ActivityId);
            if (session == null) return NotFound();

            if (!session.IsOpen) return Forbid(); // session must be open to take attendance

            // existing present users
            var existing = await _context.AttendanceRecords
                .Where(r => r.AttendanceSessionId == session.Id)
                .ToListAsync();

            // make a fast lookup
            var existingByUser = existing.ToDictionary(r => r.UserId, r => r);

            // Add any newly-present users
            foreach (var m in model.Members.Where(m => m.IsPresent))
            {
                if (!existingByUser.ContainsKey(m.UserId))
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceSessionId = session.Id,
                        UserId = m.UserId
                    });
                }
            }

            // Remove users marked absent (i.e., present before, now unchecked)
            var toRemove = existing.Where(r => !model.Members.Any(m => m.UserId == r.UserId && m.IsPresent)).ToList();
            if (toRemove.Count > 0)
                _context.AttendanceRecords.RemoveRange(toRemove);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Sessions), new { activityId = model.ActivityId });
        }
    }
}
