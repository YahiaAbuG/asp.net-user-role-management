namespace WebApplication5.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int ActivityId { get; set; }
        public Activity Activity { get; set; }

        public DateTime Date { get; set; }

        public bool IsPresent { get; set; }
    }
}
