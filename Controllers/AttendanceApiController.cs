//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using WebApplication5.Data;
//using WebApplication5.Models;
//using WebApplication5.Models.ViewModels;

//namespace WebApplication5.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AttendanceAccess")]
//    public class AttendanceApiController : ControllerBase
//    {

//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public AttendanceApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) 
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        [HttpPost("mark")]
//        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
//        {
//            if (!DateTime.TryParse(dto.Date, out var parsedDate))
//                return BadRequest("Invalid date format. Use yyyy-MM-dd.");

//            var record = await _context.AttendanceRecords
//                .FirstOrDefaultAsync(r =>
//                    r.ActivityId == dto.ActivityId &&
//                    r.UserId == dto.UserId &&
//                    r.Date.Date == parsedDate.Date);

//            if (record == null)
//            {
//                record = new AttendanceRecord
//                {
//                    ActivityId = dto.ActivityId,
//                    UserId = dto.UserId,
//                    Date = parsedDate,
//                    IsPresent = dto.IsPresent
//                };
//                _context.AttendanceRecords.Add(record);
//            }
//            else
//            {
//                record.IsPresent = dto.IsPresent;
//            }

//            await _context.SaveChangesAsync();
//            return NoContent();
//        }

//        [HttpGet("{activityId}/by-date")]
//        public async Task<IActionResult> GetAttendanceByDate(int activityId, string date)
//        {
//            if (!DateTime.TryParse(date, out var parsedDate))
//                return BadRequest("Invalid date format. Use yyyy-MM-dd.");

//            var results = await _context.AttendanceRecords
//                .Where(r => r.ActivityId == activityId && r.Date.Date == parsedDate.Date)
//                .Join(_context.Users, r => r.UserId, u => u.Id, (r, u) => new AttendanceRecordDto
//                {
//                    UserId = u.Id,
//                    UserName = u.UserName,
//                    ActivityId = r.ActivityId,
//                    Date = r.Date.ToString("yyyy-MM-dd"),
//                    IsPresent = r.IsPresent
//                })
//                .ToListAsync();

//            return Ok(results);
//        }

//        [HttpGet("{activityId}/range")]
//        public async Task<IActionResult> GetByRange(int activityId, string startDate, string endDate)
//        {
//            if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
//                return BadRequest("Invalid date format. Use yyyy-MM-dd.");

//            var results = await _context.AttendanceRecords
//                .Where(r => r.ActivityId == activityId && r.Date.Date >= start.Date && r.Date.Date <= end.Date)
//                .Join(_context.Users, r => r.UserId, u => u.Id, (r, u) => new AttendanceRecordDto
//                {
//                    UserId = u.Id,
//                    UserName = u.UserName,
//                    ActivityId = r.ActivityId,
//                    Date = r.Date.ToString("yyyy-MM-dd"),
//                    IsPresent = r.IsPresent
//                })
//                .ToListAsync();

//            return Ok(results);
//        }

//    }
//}
