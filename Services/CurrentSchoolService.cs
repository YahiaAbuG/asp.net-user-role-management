using Microsoft.Extensions.Primitives;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Services
{
    public class CurrentSchoolService : ICurrentSchoolService
    {
        public int? GetCurrentSchoolId(HttpContext context)
        {
            // First try from query string
            var queryId = context.Request.Query["schoolId"];
            if (!StringValues.IsNullOrEmpty(queryId) && int.TryParse(queryId, out var schoolIdFromQuery))
            {
                return schoolIdFromQuery;
            }

            // Optionally try route values
            var routeId = context.GetRouteValue("schoolId")?.ToString();
            if (int.TryParse(routeId, out var schoolIdFromRoute))
            {
                return schoolIdFromRoute;
            }

            return null;
        }
    }

}
