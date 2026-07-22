using ShareService.Models.Auth;

namespace ShareService.DataAccess.Interface
{
    public interface IModuleCatalog
    {
        Task<List<ModuleModel>> GetAllAsync();
    }
}
