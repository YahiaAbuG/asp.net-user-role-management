namespace WebApplication5.Models.ViewModels
{
    public class EditAttendanceViewModel
    {
        public int ActivityId { get; set; }
        public int AttendanceSessionId { get; set; } // NEW
        public DateTime Date { get; set; }
        public List<MemberAttendanceCheckbox> Members { get; set; } = new();
    }

    public class MemberAttendanceCheckbox
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsPresent { get; set; }
    }

}
