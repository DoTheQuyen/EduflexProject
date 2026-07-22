using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Permission;

namespace ShareService.DataAccess
{
    public class PermissionCatalog : IPermissionCatalog
    {
        private readonly IMongoCollection<PermissionModel> _permissionsCollection;

        public PermissionCatalog(IMongoDatabase database)
        {
            _permissionsCollection = database.GetCollection<PermissionModel>("Permissions");
        }

        public async Task<List<PermissionModel>> GetAllAsync()
        {
            return await _permissionsCollection
                .Find(FilterDefinition<PermissionModel>.Empty)
                .ToListAsync();
        }

        public async Task<List<PermissionModel>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return new List<PermissionModel>();
            }

            return await _permissionsCollection
                .Find(p => idList.Contains(p.Id))
                .ToListAsync();
        }
    }
}
