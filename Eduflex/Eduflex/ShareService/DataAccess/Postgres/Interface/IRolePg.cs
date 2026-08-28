using ShareService.Common;
using ShareService.Models.Role;

namespace ShareService.DataAccess.Postgres.Interface
{
    public interface IRolePg
    {
        Task<RoleModel?> GetByIdAsync(string roleId);
        Task<RoleModel?> GetByNameAsync(string name);
        Task<List<RoleModel>> GetAllAsync();
        Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query);
        Task<bool> CreateAsync(RoleModel role);
        Task<bool> UpdateAsync(string id, RoleModel role);
    }
}