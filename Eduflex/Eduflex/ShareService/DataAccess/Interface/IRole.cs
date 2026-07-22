using ShareService.Models.Role;

namespace ShareService.DataAccess.Interface
{
    public interface IRole
    {
        Task<RoleModel?> GetByIdAsync(string roleId);
        Task<RoleModel?> GetByNameAsync(string name);
        Task<List<RoleModel>> GetAllAsync();
        Task<RoleModel> CreateAsync(RoleModel role);
    }
}