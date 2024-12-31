using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QRCoder;

namespace WebApplication5.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string ProfileImagePath { get; set; } = "~/images/blank-profile.png";

        public void GenerateQrCode()
        {
            string qrDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/qrs");
            if (!Directory.Exists(qrDirectory))
            {
                Directory.CreateDirectory(qrDirectory);
            }

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(Id, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                    {
                        string filePath = Path.Combine(qrDirectory, $"{Id}.png");
                        qrCodeImage.Save(filePath, ImageFormat.Png);
                    }
                }
            }
        }
    }
}
