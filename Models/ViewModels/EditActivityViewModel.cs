using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models.ViewModels
{
    public class EditActivityViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Activity name is required.")]
        public string Name { get; set; }
    }
}
