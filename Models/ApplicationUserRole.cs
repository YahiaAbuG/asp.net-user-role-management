using Microsoft.AspNetCore.Identity;

namespace WebApplication5.Models
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public int SchoolId { get; set; }

        public School School { get; set; }
        public ApplicationUser User { get; set; }

        //public IdentityRole<string> Role { get; set; }
    }
   
}
