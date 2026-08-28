using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.MigrationCase;

namespace ShareService.DataAccess
{
    public class MigrationCase : AuditableCollectionBase<MigrationCaseModel>, IMigrationCase
    {
        public MigrationCase(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<MigrationCaseModel>("MigrationCases"), currentUser)
        {
        }

        public async Task<bool> CreateCaseAsync(MigrationCaseModel migrationCase)
        {
            await InsertOneAsync(migrationCase);
            return true;
        }

        public async Task<MigrationCaseModel?> GetCaseAsync(string id)
        {
            return await Collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public Task<PagedResult<MigrationCaseModel>> GetCasesAsync(MigrationCaseFilter filter)
        {
            var filters = new List<FilterDefinition<MigrationCaseModel>>
            {
                BuildSearchFilter(filter.SearchTerm, c => c.CaseReference, c => c.PrimaryContactName)
            };

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                filters.Add(Builders<MigrationCaseModel>.Filter.In(c => c.Status, filter.Statuses));
            }

            if (!string.IsNullOrWhiteSpace(filter.OwnerUserId))
            {
                filters.Add(Builders<MigrationCaseModel>.Filter.Eq(c => c.OwnerUserId, filter.OwnerUserId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                filters.Add(Builders<MigrationCaseModel>.Filter.Eq(c => c.Category, filter.Category));
            }

            var mongoFilter = Builders<MigrationCaseModel>.Filter.And(filters);
            var sort = Builders<MigrationCaseModel>.Sort.Descending(c => c.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        // Used only to generate the next CaseReference number — see
        // MigrationCaseService.GenerateCaseReferenceAsync.
        public async Task<long> CountAllAsync()
        {
            return await Collection.CountDocumentsAsync(FilterDefinition<MigrationCaseModel>.Empty);
        }

        public async Task<bool> ReplaceCaseAsync(string id, MigrationCaseModel migrationCase)
        {
            return await ReplaceOneAsync(c => c.Id == id, migrationCase);
        }

        public async Task<Dictionary<string, int>> GetMonthlyCountsAsync(DateTime since)
        {
            var results = await Collection.Aggregate()
                .Match(c => c.CreatedAt >= since)
                .Group(c => new { c.CreatedAt.Year, c.CreatedAt.Month }, g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            return results.ToDictionary(r => $"{r.Year:D4}-{r.Month:D2}", r => r.Count);
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync()
        {
            var results = await Collection.Aggregate()
                .Group(c => c.Status, g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return results.ToDictionary(r => r.Status, r => r.Count);
        }
    }
}
