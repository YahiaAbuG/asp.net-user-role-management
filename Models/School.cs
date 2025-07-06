namespace WebApplication5.Models
{
    public class School
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
