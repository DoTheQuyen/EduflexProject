using Microsoft.EntityFrameworkCore;
using ShareService.Common;
using ShareService.DataAccess.Postgres;
using ShareService.Models.Common;

namespace ShareService.DataAccess.Postgres.Common
{

    // Base class for auditable entities, providing common CRUD operations with audit fields
    // T is constrained to be a class that implements IAuditableEntity
    // This class is intended to be inherited by specific entity repositories that need to handle auditable entities
    // It provides methods for inserting, replacing, saving, deleting, and paginating entities while automatically managing audit fields like CreatedAt, CreatedBy, UpdatedAt, and UpdatedBy
    // All methods are asynchronous and interact with the underlying DbContext to persist changes to the database.
    // None of any functions in DBAccess are allowed to be interact with the database directly, they should be used through the derived classes that implement specific entity logic.
    public abstract class AuditableDbSetBase<T> where T : class, IAuditableEntity
    {
        protected readonly DbSet<T> EntitySet;
        protected string? CurrentUserId => _currentUser.UserId;
        private readonly EduflexPostgresContext _context;
        private readonly ICurrentUserService _currentUser;

        protected AuditableDbSetBase(EduflexPostgresContext context, DbSet<T> entitySet, ICurrentUserService currentUser)
        {
            _context = context;
            EntitySet = entitySet;
            _currentUser = currentUser;
        }

        protected async Task InsertAsync(T entity)
        {
            var now = DateTime.UtcNow;
            entity.CreatedAt = now;
            entity.CreatedBy = _currentUser.UserId;
            entity.UpdatedAt = now;
            entity.UpdatedBy = _currentUser.UserId;

            EntitySet.Add(entity);
            await _context.SaveChangesAsync();
        }

        protected async Task<bool> ReplaceAsync(T existing, T replacement)
        {
            //var existing = await EntitySet.FirstOrDefaultAsync(r => r.Id == id);
            //existing.Name = newName;
            //await _context.SaveChangesAsync();

            replacement.CreatedAt = existing.CreatedAt;
            replacement.CreatedBy = existing.CreatedBy;
            replacement.UpdatedAt = DateTime.UtcNow;
            replacement.UpdatedBy = _currentUser.UserId;

            _context.Entry(existing).CurrentValues.SetValues(replacement);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        protected async Task<bool> SaveWithStampAsync(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        protected async Task<bool> DeleteAsync(T entity)
        {
            EntitySet.Remove(entity);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        protected async Task<PagedResult<T>> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}