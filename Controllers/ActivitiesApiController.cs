using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication5.Data;
using WebApplication5.Models;

namespace WebApplication5.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Ensure user is authenticated
    public class ActivitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetActivityById(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Get all roles for the current user related to this activity
            var userRolesForActivity = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.ActivityId == id)
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => r.Name)
                .ToListAsync();

            var isActivityAdmin = userRolesForActivity.Contains("ActivityAdmin");

            if (!isActivityAdmin)
            {
                // Get all authorized users for this activity (ActivityAdmins only)
                var authorizedUserIds = await _context.UserRoles
                    .Where(ur => ur.ActivityId == id)
                    .Join(_context.Roles,
                          ur => ur.RoleId,
                          r => r.Id,
                          (ur, r) => new { ur.UserId, r.Name })
                    .Where(x => x.Name == "ActivityAdmin")
                    .Select(x => x.UserId)
                    .ToListAsync();

                return Forbid($"Access denied. User ID: {userId}, Roles for this activity: [{string.Join(", ", userRolesForActivity)}]. " +
                              $"Authorized ActivityAdmins for activity {id}: [{string.Join(", ", authorizedUserIds)}]");
            }

            var activity = await _context.Activity.FindAsync(id);
            if (activity == null)
            {
                return NotFound("Activity not found.");
            }

            return Ok(activity);
        }

    }
}
