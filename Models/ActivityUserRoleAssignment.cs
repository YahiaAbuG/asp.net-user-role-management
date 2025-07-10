namespace WebApplication5.Models
{
    public class ActivityUserRoleAssignment
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string? CurrentRole { get; set; } // "ActivityAdmin", "ActivityMember", or null
    }
}
