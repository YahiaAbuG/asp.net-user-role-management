namespace WebApplication5.Models.ViewModels
{
    public class AttendanceReportViewModel
    {
        public string ActivityName { get; set; }
        public List<DateTime> Dates { get; set; }
        public List<MemberAttendanceRow> Members { get; set; }
    }

    public class MemberAttendanceRow
    {
        public string Name { get; set; }
        public List<bool> AttendancePerDate { get; set; }

        public int DaysAttended => AttendancePerDate.Count(x => x);
        public int TotalDays => AttendancePerDate.Count;
    }
}
