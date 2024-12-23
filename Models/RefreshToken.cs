using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    }
}
