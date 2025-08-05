using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models;
using System.Security.Claims;
using WebApplication5.Requirements;
using WebApplication5.Services;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Handlers
{
    public class AttendanceAccessHandler : AuthorizationHandler<AttendanceAccessRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISchoolRoleService _schoolRoleService;

        public AttendanceAccessHandler(ApplicationDbContext context,
                                       IHttpContextAccessor httpContextAccessor,
                                       UserManager<ApplicationUser> userManager,
                                       ISchoolRoleService schoolRoleService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _schoolRoleService = schoolRoleService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                              AttendanceAccessRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var routeValues = httpContext?.Request.RouteValues;

            if (routeValues == null || !routeValues.TryGetValue("activityId", out var idValue))
                return;

            if (!int.TryParse(idValue?.ToString(), out var activityId))
                return;

            var userId = _userManager.GetUserId(httpContext.User);
            if (string.IsNullOrEmpty(userId))
                return;

            // Check SuperAdmin
            var isSuperAdmin = await _schoolRoleService.IsUserSuperAdminAsync(userId);
            if (isSuperAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Check ActivityAdmin
            var activityAdminRoleId = await _context.Roles
                .Where(r => r.Name == "ActivityAdmin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var isActivityAdmin = await _context.UserRoles.AnyAsync(ur =>
                ur.UserId == userId && ur.ActivityId == activityId && ur.RoleId == activityAdminRoleId);

            if (isActivityAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Check SchoolAdmin for that activity's school
            var schoolId = await _context.Activity
                .Where(a => a.Id == activityId)
                .Select(a => a.SchoolId)
                .FirstOrDefaultAsync();

            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var isSchoolAdmin = await _context.UserRoles.AnyAsync(ur =>
                ur.UserId == userId && ur.SchoolId == schoolId && ur.RoleId == adminRoleId);

            if (isSchoolAdmin)
            {
                context.Succeed(requirement);
            }
        }
    }
}
