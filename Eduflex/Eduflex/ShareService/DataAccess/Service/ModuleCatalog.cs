using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;

namespace ShareService.DataAccess
{
    public class ModuleCatalog : IModuleCatalog
    {
        private readonly IMongoCollection<ModuleModel> _modulesCollection;

        public ModuleCatalog(IMongoDatabase database)
        {
            _modulesCollection = database.GetCollection<ModuleModel>("Modules");
        }

        public async Task<List<ModuleModel>> GetAllAsync()
        {
            return await _modulesCollection
                .Find(FilterDefinition<ModuleModel>.Empty)
                .ToListAsync();
        }
    }
}
