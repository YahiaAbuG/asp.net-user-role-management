using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication5.Models.Interfaces;
using WebApplication5.Services;

namespace WebApplication5.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeSchoolRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredRole;

        public AuthorizeSchoolRoleAttribute(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
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

            var isInRole = roleService.IsUserInRoleAsync(userId, _requiredRole, schoolId).GetAwaiter().GetResult();

            if (!isInRole)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
