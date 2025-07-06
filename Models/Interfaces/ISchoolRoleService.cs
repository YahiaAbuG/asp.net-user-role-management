namespace WebApplication5.Models.Interfaces
{
    public interface ISchoolRoleService
    {
        Task<List<string>> GetUserRolesAsync(string userId, int schoolId);
        Task<bool> IsUserInRoleAsync(string userId, string roleName, int schoolId);
        Task AssignRolesAsync(string userId, IEnumerable<string> roleNames, int schoolId);
        Task RemoveRolesAsync(string userId, int schoolId);
        Task<bool> IsRoleInUse(string roleId);
    }
}
