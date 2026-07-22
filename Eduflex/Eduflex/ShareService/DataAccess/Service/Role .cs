using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Role;

namespace ShareService.DataAccess
{
    public class Role : IRole
    {
        private readonly IMongoCollection<RoleModel> _rolesCollection;

        public Role(IMongoDatabase database)
        {
            _rolesCollection = database.GetCollection<RoleModel>("Roles");
        }

        public async Task<RoleModel?> GetByIdAsync(string roleId)
        {
            return await _rolesCollection
                .Find(r => r.Id == roleId)
                .FirstOrDefaultAsync();
        }

        public async Task<RoleModel?> GetByNameAsync(string name)
        {
            return await _rolesCollection
                .Find(r => r.Name == name)
                .FirstOrDefaultAsync();
        }

        public async Task<List<RoleModel>> GetAllAsync()
        {
            return await _rolesCollection
                .Find(FilterDefinition<RoleModel>.Empty)
                .ToListAsync();
        }

        public async Task<RoleModel> CreateAsync(RoleModel role)
        {
            await _rolesCollection.InsertOneAsync(role);
            return role;
        }
    }
}