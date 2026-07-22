using ShareService.Models.Role;

namespace ShareService.Services.Interface
{
    public interface IRoleService
    {
        Task<RoleModel?> GetByIdAsync(string roleId);
        Task<List<string>> GetPermissionsAsync(string roleId);
        Task<List<RoleModel>> GetAllRolesAsync();
        Task<RoleModel> CreateRoleAsync(CreateRoleModel createRoleModel);
    }
}