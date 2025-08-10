using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models.ViewModels
{
    public class MarkAttendanceDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [Required]
        public string Date { get; set; }

        [Required]
        public bool IsPresent { get; set; }
    }
}