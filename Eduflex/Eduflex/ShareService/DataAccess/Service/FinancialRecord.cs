using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Financial;

namespace ShareService.DataAccess
{
    public class FinancialRecord : AuditableCollectionBase<FinancialRecordModel>, IFinancialRecord
    {
        public FinancialRecord(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<FinancialRecordModel>("FinancialRecords"), currentUser)
        {
        }

        public async Task<bool> CreateAsync(FinancialRecordModel record)
        {
            await InsertOneAsync(record);
            return true;
        }

        public async Task<FinancialRecordModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task<FinancialRecordModel?> GetByEnrolmentIdAsync(string enrolmentId)
        {
            return await Collection.Find(r => r.EnrolmentId == enrolmentId).FirstOrDefaultAsync();
        }

        public Task<PagedResult<FinancialRecordModel>> GetFinancialRecordsAsync(FinancialRecordFilter filter)
        {
            var mongoFilter = FilterDefinition<FinancialRecordModel>.Empty;
            var sort = Builders<FinancialRecordModel>.Sort.Descending(r => r.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<List<FinancialRecordModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<FinancialRecordModel>.Empty).ToListAsync();
        }

        public async Task<bool> ReplaceAsync(string id, FinancialRecordModel record)
        {
            return await ReplaceOneAsync(r => r.Id == id, record);
        }
    }
}
