namespace WebApplication5.Models.ViewModels
{
    public class AttendanceRecordDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int ActivityId { get; set; }
        public string Date { get; set; } // yyyy-MM-dd
        public bool IsPresent { get; set; }
    }
}
