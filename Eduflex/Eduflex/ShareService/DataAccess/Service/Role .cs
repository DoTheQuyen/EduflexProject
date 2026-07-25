using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Role;

namespace ShareService.DataAccess
{
    public class Role : AuditableCollectionBase<RoleModel>, IRole
    {
        public Role(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<RoleModel>("Roles"), currentUser)
        {
        }

        public async Task<RoleModel?> GetByIdAsync(string roleId)
        {
            return await Collection
                .Find(r => r.Id == roleId)
                .FirstOrDefaultAsync();
        }

        public async Task<RoleModel?> GetByNameAsync(string name)
        {
            return await Collection
                .Find(r => r.Name == name)
                .FirstOrDefaultAsync();
        }

        public async Task<List<RoleModel>> GetAllAsync()
        {
            return await Collection
                .Find(FilterDefinition<RoleModel>.Empty)
                .ToListAsync();
        }

        public Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query)
        {
            var filter = BuildSearchFilter(query.SearchTerm, r => r.Name);
            var sort = Builders<RoleModel>.Sort.Ascending(r => r.Name);

            return GetPagedAsync(filter, sort, query.PageNumber, query.PageSize);
        }

        public async Task<bool> CreateAsync(RoleModel role)
        {
            await InsertOneAsync(role);
            return true;
        }
    }
}
