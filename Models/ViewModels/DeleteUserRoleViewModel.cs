namespace WebApplication5.Models.ViewModels
{
    public class DeleteUserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public string RoleId { get; set; }
        public string RoleName { get; set; }

        public int? SchoolId { get; set; }
        public string SchoolName { get; set; }

        public int? ActivityId { get; set; }
        public string ActivityName { get; set; }
    }
}
