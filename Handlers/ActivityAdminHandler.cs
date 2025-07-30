using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication5.Data;
using WebApplication5.Models;
using WebApplication5.Requirements;

namespace WebApplication5.Handlers
{

    public class ActivityAdminHandler : AuthorizationHandler<ActivityAdminRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityAdminHandler(ApplicationDbContext context,
                                    IHttpContextAccessor httpContextAccessor,
                                    UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                              ActivityAdminRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var routeValues = httpContext?.Request.RouteValues;

            if (routeValues == null || !routeValues.TryGetValue("id", out var idValue))
                return;

            if (!int.TryParse(idValue?.ToString(), out var activityId))
                return;

            var userId = _userManager.GetUserId(httpContext.User);
            if (string.IsNullOrEmpty(userId))
                return;

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.ActivityId == activityId)
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => r.Name)
                .ToListAsync();

            if (userRoles.Contains("ActivityAdmin"))
            {
                context.Succeed(requirement);
            }
        }
    }
}
