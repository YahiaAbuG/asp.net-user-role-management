using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebApplication5.Data;
using WebApplication5.Models;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Services
{
    public class SchoolRoleService : ISchoolRoleService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SchoolRoleService(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<List<string>> GetUserRolesAsync(string userId, int schoolId)
        {
            // Get RoleIds assigned to the user in this school
            var roleIds = await _context.UserRoles
                .Where(r => r.UserId == userId && r.SchoolId == schoolId)
                .Select(r => r.RoleId)
                .ToListAsync();

            // Get role names from RoleManager using those RoleIds
            return await _roleManager.Roles
                .Where(role => roleIds.Contains(role.Id))
                .Select(r => r.Name)
                .ToListAsync();
        }


        public async Task<bool> IsUserInRoleAsync(string userId, string roleName, int schoolId)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) return false;

            if (roleName == "SuperAdmin")
            {
                return await _context.UserRoles.AnyAsync(r =>
                    r.UserId == userId &&
                    r.RoleId == role.Id
                );
            }

            return await _context.UserRoles.AnyAsync(r =>
                r.UserId == userId &&
                r.SchoolId == schoolId &&
                r.RoleId == role.Id);
        }


        public async Task<bool> IsUserSuperAdminAsync(string userId)
        {

            var superAdmin = await _roleManager.FindByNameAsync("SuperAdmin");

            return await _context.UserRoles.AnyAsync(r =>
                    r.UserId == userId &&
                    r.RoleId == superAdmin.Id
                );
        }


        public async Task AssignRolesAsync(string userId, IEnumerable<string> roleNames, int schoolId)
        {
            var existingRoles = _context.UserRoles
                .Where(r => r.UserId == userId && r.SchoolId == schoolId);

            _context.UserRoles.RemoveRange(existingRoles);

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    _context.UserRoles.Add(new ApplicationUserRole
                    {
                        UserId = userId,
                        RoleId = role.Id,
                        SchoolId = schoolId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveRolesAsync(string userId, int schoolId)
        {
            var roles = _context.UserRoles
                .Where(r => r.UserId == userId && r.SchoolId == schoolId);
            _context.UserRoles.RemoveRange(roles);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsRoleInUse(string roleId)
        {
            return await _context.UserRoles.AnyAsync(r => r.RoleId == roleId);
        }
        public async Task<List<School>> GetAllSchoolsAsync()
        {
            return await _context.Schools.ToListAsync();
        }

    }
}
