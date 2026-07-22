using ShareService.Models.Permission;

namespace ShareService.DataAccess.Interface
{
    public interface IPermissionCatalog
    {
        Task<List<PermissionModel>> GetAllAsync();
        Task<List<PermissionModel>> GetByIdsAsync(IEnumerable<string> ids);
    }
}
