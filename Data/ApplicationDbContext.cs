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
                b.HasKey(r => new { r.UserId, r.RoleId });

                b.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .IsRequired();

                b.HasOne(r => r.School)
                    .WithMany(s => s.UserRoles)
                    .HasForeignKey(r => r.SchoolId)
                    .IsRequired(false) // SchoolId is nullable
                    .OnDelete(DeleteBehavior.Cascade);
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
            if (!await roleManager.RoleExistsAsync("SuperAdmin"))
            {
                await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            }
        }

        public static async Task SeedDefaultAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            // Ensure all roles are seeded
            await SeedRolesAsync(roleManager);

            // Create the SuperAdmin user if it doesn't exist
            var superAdmin = new ApplicationUser
            {
                UserName = "superadmin",
                Email = "super@admin.com",
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            // Check if user already exists
            var user = await userManager.FindByEmailAsync(superAdmin.Email);
            if (user == null)
            {
                var result = await userManager.CreateAsync(superAdmin, "Blud456");
                if (!result.Succeeded)
                    return; // or handle error
                user = superAdmin;
            }

            // Ensure SuperAdmin role exists
            var role = await roleManager.FindByNameAsync("SuperAdmin");
            if (role == null)
            {
                await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
                role = await roleManager.FindByNameAsync("SuperAdmin");
            }

            // Manually assign SuperAdmin role without SchoolId
            var existingRole = await dbContext.UserRoles.FirstOrDefaultAsync(r =>
                r.UserId == user.Id &&
                r.RoleId == role.Id &&
                r.SchoolId == null);

            if (existingRole == null)
            {
                dbContext.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    SchoolId = null // Indicates global role
                });

                await dbContext.SaveChangesAsync();
            }
        }


        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
