using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AttendanceAccess")]
    public class AttendanceApiController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) 
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{activityId}/date/{date}")]
        public async Task<IActionResult> GetAttendanceByDate(int activityId, DateTime date)
        {
            var records = await _context.AttendanceRecords
                .Include(ar => ar.User)
                .Where(ar => ar.ActivityId == activityId && ar.Date.Date == date.Date)
                .ToListAsync();

            return Ok(records.Select(r => new
            {
                r.UserId,
                r.User.UserName,
                r.IsPresent
            }));
        }

    }
}
