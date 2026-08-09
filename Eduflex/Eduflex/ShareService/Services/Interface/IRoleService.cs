using ShareService.Common;
using ShareService.Models.Role;

namespace ShareService.Services.Interface
{
    public interface IRoleService
    {
        Task<RoleModel?> GetByIdAsync(string roleId);
        Task<RoleModel?> GetByNameAsync(string name);
        Task<List<string>> GetPermissionsAsync(string roleId);
        Task<List<RoleModel>> GetAllRolesAsync();
        Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query, string userId);
        Task<bool> CreateRoleAsync(RoleModel role, string userId);
        Task<bool> UpdateRoleAsync(string id, RoleModel role, string userId);
    }
}