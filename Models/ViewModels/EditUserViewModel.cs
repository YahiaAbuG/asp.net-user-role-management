﻿using System.ComponentModel.DataAnnotations;

namespace WebApplication5.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string UserName { get; set; }
        public IFormFile ProfileImage { get; set; } // Property for the image file
    }
}
