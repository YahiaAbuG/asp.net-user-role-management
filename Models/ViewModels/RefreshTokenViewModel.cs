using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models.ViewModels
{
    public class RefreshTokenViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
