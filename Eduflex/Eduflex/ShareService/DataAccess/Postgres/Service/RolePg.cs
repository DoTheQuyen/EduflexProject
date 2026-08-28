using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using ShareService.Common;
using ShareService.DataAccess.Postgres.Common;
using ShareService.DataAccess.Postgres.Interface;
using ShareService.Models.Role;

namespace ShareService.DataAccess.Postgres.Service
{
    public class RolePg : AuditableDbSetBase<RoleModel>, IRolePg
    {
        private readonly EduflexPostgresContext _context;

        public RolePg(EduflexPostgresContext context, ICurrentUserService currentUser)
            : base(context, context.Roles, currentUser)
        {
            _context = context;
        }

        public async Task<RoleModel?> GetByIdAsync(string roleId)
        {
            return await EntitySet.FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task<RoleModel?> GetByNameAsync(string name)
        {
            return await EntitySet.FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<List<RoleModel>> GetAllAsync()
        {
            return await EntitySet.ToListAsync();
        }

        public Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query)
        {
            var roles = EntitySet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                roles = roles.Where(r => EF.Functions.ILike(r.Name, $"%{query.SearchTerm}%"));
            }

            roles = roles.OrderBy(r => r.Name);

            return GetPagedAsync(roles, query.PageNumber, query.PageSize);
        }

        public async Task<bool> CreateAsync(RoleModel role)
        {
            if (string.IsNullOrEmpty(role.Id))
            {
                role.Id = ObjectId.GenerateNewId().ToString();
            }

            await InsertAsync(role);
            return true;
        }

        public async Task<bool> UpdateAsync(string id, RoleModel role)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(r => r.Id == id);
            if (existing == null)
            {
                return false;
            }

            return await ReplaceAsync(existing, role);
        }
    }
}