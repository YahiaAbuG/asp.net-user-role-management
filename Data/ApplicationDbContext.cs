using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser, IdentityRole, string, 
        IdentityUserClaim<string>, ApplicationUserRole, 
        IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
    {

        public DbSet<School> Schools { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUserRole>(b =>
            {
                b.HasKey(r => new { r.UserId, r.RoleId, r.SchoolId });

                b.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .IsRequired();

                b.HasOne(r => r.School)
                    .WithMany(s => s.UserRoles)
                    .HasForeignKey(r => r.SchoolId)
                    .IsRequired();
            });

            builder.Entity<School>().HasData(
                new School { Id = 1, Name = "School 1" },
                new School { Id = 2, Name = "School 2" },
                new School { Id = 3, Name = "School 3" }
            );
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Check if roles exist, if not, create them
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("Manager"))
            {
                await roleManager.CreateAsync(new IdentityRole("Manager"));
            }
            if (!await roleManager.RoleExistsAsync("Member"))
            {
                await roleManager.CreateAsync(new IdentityRole("Member"));
            }
        }

        public static async Task SeedDefaultAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed roles
            await SeedRolesAsync(roleManager);

            // Create default admin user if it doesn't exist
            var defaultUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.UserName != defaultUser.UserName))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "Blud456");
                    await userManager.AddToRoleAsync(defaultUser, "Admin");
                }
            }
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
