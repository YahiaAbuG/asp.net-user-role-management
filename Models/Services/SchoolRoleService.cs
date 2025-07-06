using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Data;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Models.Services
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
            return await _context.UserRoles
                .Include(r => r.Role)
                .Where(r => r.UserId == userId && r.SchoolId == schoolId)
                .Select(r => r.Role.Name)
                .ToListAsync();
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName, int schoolId)
        {
            return await _context.UserRoles
                .Include(r => r.Role)
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.SchoolId == schoolId &&
                    r.Role.Name == roleName);
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
    }
}
