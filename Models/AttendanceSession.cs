namespace WebApplication5.Models
{
    public class AttendanceSession
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }
        public Activity Activity { get; set; }
        public DateTime Date { get; set; }
        public bool IsOpen { get; set; } = true;
        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }
}
