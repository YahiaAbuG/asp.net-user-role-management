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
                b.HasKey(r => r.Id); // single-column primary key

                b.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .IsRequired();

                b.HasOne(r => r.School)
                    .WithMany(s => s.UserRoles)
                    .HasForeignKey(r => r.SchoolId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(r => r.Activity)
                    .WithMany()
                    .HasForeignKey(r => r.ActivityId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional: prevent duplicate combinations
                b.HasIndex(r => new { r.UserId, r.RoleId, r.SchoolId, r.ActivityId }).IsUnique();

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
            if (!await roleManager.RoleExistsAsync("ActivityAdmin"))
            {
                await roleManager.CreateAsync(new IdentityRole("ActivityAdmin"));
            }
            if (!await roleManager.RoleExistsAsync("ActivityMember"))
            {
                await roleManager.CreateAsync(new IdentityRole("ActivityMember"));
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

        public static async Task SeedAttendanceDatesAsync(ApplicationDbContext dbContext)
        {
            var activities = await dbContext.Activity.ToListAsync();

            var startDate = new DateTime(2025, 8, 3);
            var endDate = new DateTime(2025, 8, 7);
            var dateRange = Enumerable.Range(0, (endDate - startDate).Days + 1)
                                      .Select(offset => startDate.AddDays(offset))
                                      .ToList();

            foreach (var activity in activities)
            {
                foreach (var date in dateRange)
                {
                    var exists = await dbContext.AttendanceRecords.AnyAsync(ar =>
                        ar.ActivityId == activity.Id && ar.Date.Date == date.Date);

                    if (!exists)
                    {
                        var activityMemberRoleId = await dbContext.Roles
                            .Where(r => r.Name == "ActivityMember")
                            .Select(r => r.Id)
                            .FirstOrDefaultAsync();

                        var members = await dbContext.UserRoles
                            .Where(ur => ur.ActivityId == activity.Id && ur.RoleId == activityMemberRoleId)
                            .Select(ur => ur.UserId)
                            .ToListAsync();

                        foreach (var memberId in members)
                        {
                            dbContext.AttendanceRecords.Add(new AttendanceRecord
                            {
                                ActivityId = activity.Id,
                                UserId = memberId,
                                Date = date,
                                IsPresent = false
                            });
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Activity> Activity { get; set; } = default!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = default!;
    }
}
