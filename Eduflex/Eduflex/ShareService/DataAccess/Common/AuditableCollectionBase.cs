using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.Common;
using ShareService.Models.Common;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShareService.DataAccess.Common
{
    public abstract class AuditableCollectionBase<T> where T : IAuditableEntity
    {
        protected readonly IMongoCollection<T> Collection;
        private readonly ICurrentUserService _currentUser;

        protected AuditableCollectionBase(IMongoCollection<T> collection, ICurrentUserService currentUser)
        {
            Collection = collection;
            _currentUser = currentUser;
        }

        protected async Task InsertOneAsync(T entity, IClientSessionHandle? session = null)
        {
            var now = DateTime.UtcNow;
            entity.CreatedAt = now;
            entity.CreatedBy = _currentUser.UserId;
            entity.UpdatedAt = now;
            entity.UpdatedBy = _currentUser.UserId;

            if (session == null)
                await Collection.InsertOneAsync(entity);
            else
                await Collection.InsertOneAsync(session, entity);
        }

        protected async Task<UpdateResult> UpdateOneAsync(
            Expression<Func<T, bool>> filter,
            UpdateDefinition<T> update,
            IClientSessionHandle? session = null)
        {
            FilterDefinition<T> filterDefinition = filter;
            var stamped = WithAuditStamp(update);

            return session == null
                ? await Collection.UpdateOneAsync(filterDefinition, stamped)
                : await Collection.UpdateOneAsync(session, filterDefinition, stamped);
        }

        protected async Task<T> FindOneAndUpdateAsync(
            Expression<Func<T, bool>> filter,
            UpdateDefinition<T> update,
            FindOneAndUpdateOptions<T>? options = null)
        {
            FilterDefinition<T> filterDefinition = filter;
            var stamped = WithAuditStamp(update);
            return await Collection.FindOneAndUpdateAsync(filterDefinition, stamped, options);
        }

        protected async Task<T> FindOneAndReplaceAsync(
            Expression<Func<T, bool>> filter,
            T replacement,
            IClientSessionHandle? session = null)
        {
            FilterDefinition<T> filterDefinition = filter;
            replacement.UpdatedAt = DateTime.UtcNow;
            replacement.UpdatedBy = _currentUser.UserId;

            var options = new FindOneAndReplaceOptions<T> { ReturnDocument = ReturnDocument.After };

            return session == null
                ? await Collection.FindOneAndReplaceAsync(filterDefinition, replacement, options)
                : await Collection.FindOneAndReplaceAsync(session, filterDefinition, replacement, options);
        }

        protected async Task<bool> ReplaceOneAsync(
            Expression<Func<T, bool>> filter,
            T replacement,
            IClientSessionHandle? session = null)
        {
            FilterDefinition<T> filterDefinition = filter;
            replacement.UpdatedAt = DateTime.UtcNow;
            replacement.UpdatedBy = _currentUser.UserId;

            var result = session == null
                ? await Collection.ReplaceOneAsync(filterDefinition, replacement)
                : await Collection.ReplaceOneAsync(session, filterDefinition, replacement);

            return result.ModifiedCount > 0;
        }

        protected async Task<PagedResult<T>> GetPagedAsync(
            FilterDefinition<T> filter,
            SortDefinition<T> sort,
            int pageNumber,
            int pageSize)
        {
            var totalCount = await Collection.CountDocumentsAsync(filter);

            var items = await Collection
                .Find(filter)
                .Sort(sort)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Case-insensitive OR-of-regex filter across the given string fields. Empty/whitespace
        /// search terms match everything (returns FilterDefinition.Empty), so callers can always
        /// combine this into a Filter.And(...) without special-casing "no search" themselves.
        /// </summary>
        protected FilterDefinition<T> BuildSearchFilter(string? searchTerm, params Expression<Func<T, string>>[] fields)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || fields.Length == 0)
            {
                return FilterDefinition<T>.Empty;
            }

            var pattern = new BsonRegularExpression(Regex.Escape(searchTerm), "i");
            var filters = fields.Select(field =>
                Builders<T>.Filter.Regex(new ExpressionFieldDefinition<T, string>(field), pattern));
            return Builders<T>.Filter.Or(filters);
        }

        private UpdateDefinition<T> WithAuditStamp(UpdateDefinition<T> update)
        {
            return Builders<T>.Update.Combine(
                update,
                Builders<T>.Update.Set(e => e.UpdatedAt, DateTime.UtcNow),
                Builders<T>.Update.Set(e => e.UpdatedBy, _currentUser.UserId));
        }
    }
}
