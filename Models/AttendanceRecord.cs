namespace WebApplication5.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        public int AttendanceSessionId { get; set; }
        public AttendanceSession AttendanceSession { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
