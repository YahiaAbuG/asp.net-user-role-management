using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication5.Models.ViewModels
{
    public class UserRoleSummaryViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public List<string> GeneralRoles { get; set; } = new();
        public List<string> UserGeneralRoles { get; set; } = new(); // currently assigned general roles

        public List<UserSchoolRoleEntry> SchoolRoles { get; set; } = new();
        public List<UserActivityRoleEntry> ActivityRoles { get; set; } = new();

        public string SelectedRole { get; set; }
        public int? SelectedSchoolId { get; set; }
        public int? SelectedActivityId { get; set; }

        public List<SelectListItem> Schools { get; set; } = new();
        public List<SelectListItem> Activities { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class UserSchoolRoleEntry
    {
        public string SchoolName { get; set; }
        public string RoleName { get; set; }
    }

    public class UserActivityRoleEntry
    {
        public string SchoolName { get; set; }
        public string ActivityName { get; set; }
        public string RoleName { get; set; }
    }
}
