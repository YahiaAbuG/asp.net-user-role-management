using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication5.Models.Interfaces;
using WebApplication5.Services;

namespace WebApplication5.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeSchoolRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly List<string> _requiredRoles;

        public AuthorizeSchoolRoleAttribute(string requiredRoles)
        {
            _requiredRoles = requiredRoles
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(role => role.Trim())
                .ToList();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (user.IsInRole("SuperAdmin"))
            {
                // SuperAdmins are allowed to access everything
                return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            int schoolId = int.Parse(context.HttpContext.Request.Query["schoolId"]);

            if (schoolId <= 0)
            {
                context.Result = new BadRequestObjectResult("Missing or invalid schoolId");
                return;
            }

            var roleService = context.HttpContext.RequestServices.GetService<ISchoolRoleService>();

            var isInRole = false;

            foreach (var role in _requiredRoles)
            {
                if (roleService.IsUserInRoleAsync(userId, role, schoolId).GetAwaiter().GetResult())
                    isInRole = true;
            }

            if (!isInRole)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
