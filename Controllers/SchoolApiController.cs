using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;

namespace WebApplication5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Ensure user is authenticated
    public class SchoolApiController : ControllerBase
    {

        private readonly ApplicationDbContext _context;

        public SchoolApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{schoolId}")]
        [Authorize(Policy = "SchoolAdmin")]
        public async Task<IActionResult> ListAdminsBySchoolId(int schoolId)
        {
            var adminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Admin");

            var adminUsers = await _context.UserRoles
                .Where(ur => ur.SchoolId == schoolId && ur.RoleId == adminRole.Id)
                .Join(_context.Users,
                      ur => ur.UserId,
                      user => user.Id,
                      (ur, user) => new
                      {
                          user.Id,
                          user.UserName,
                          user.Email
                      })
                .ToListAsync();

            return Ok(adminUsers);
        }
    }
}
