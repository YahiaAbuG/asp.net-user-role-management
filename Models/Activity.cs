namespace WebApplication5.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int SchoolId { get; set; }
        public School School { get; set; }
    }
}
