namespace WebApplication5.Models.ViewModels
{
    public class EditAttendanceViewModel
    {
        public int ActivityId { get; set; }
        public DateTime Date { get; set; }
        public List<MemberAttendanceCheckbox> Members { get; set; }
    }

    public class MemberAttendanceCheckbox
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsPresent { get; set; }
    }

}
