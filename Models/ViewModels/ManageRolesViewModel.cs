using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models.ViewModels
{
    public class ManageRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public List<RoleCheckboxViewModel> GeneralRoles { get; set; } = new();
        public List<UserRoleDisplayViewModel> UserRolesTable { get; set; } = new();

        public RoleAssignmentForm Form { get; set; } = new();
    }

    public class RoleCheckboxViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }

    public class UserRoleDisplayViewModel
    {
        public string SchoolName { get; set; }
        public string ActivityName { get; set; } // null or "N/A" for school roles
        public string RoleName { get; set; }

        // Hidden metadata for removal
        public int? SchoolId { get; set; }
        public int? ActivityId { get; set; }
        public string RoleId { get; set; }
    }

    public class RoleAssignmentForm
    {
        [Required]
        public int SelectedSchoolId { get; set; }

        [Required]
        public string SelectedRoleName { get; set; }

        public int? SelectedActivityId { get; set; }

        public List<SelectListItem> AvailableSchools { get; set; } = new();
        public List<SelectListItem> AvailableRoles { get; set; } = new();
        public List<SelectListItem> AvailableActivities { get; set; } = new();
    }

}
