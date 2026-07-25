using ShareService.Common;
using ShareService.Models.Role;

namespace ShareService.Services.Interface
{
    public interface IRoleService
    {
        Task<RoleModel?> GetByIdAsync(string roleId);
        Task<List<string>> GetPermissionsAsync(string roleId);
        Task<List<RoleModel>> GetAllRolesAsync();
        Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query);
        Task<bool> CreateRoleAsync(RoleModel role);
    }
}
