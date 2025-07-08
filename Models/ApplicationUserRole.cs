using Microsoft.AspNetCore.Identity;

namespace WebApplication5.Models
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public int Id { get; set; } // NEW: primary key

        public int? SchoolId { get; set; }
        public School? School { get; set; }

        public int? ActivityId { get; set; }
        public Activity? Activity { get; set; }

        public ApplicationUser User { get; set; }
    }


}
