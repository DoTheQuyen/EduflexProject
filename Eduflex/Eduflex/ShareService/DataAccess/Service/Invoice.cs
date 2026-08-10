using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Invoice;

namespace ShareService.DataAccess
{
    public class Invoice : AuditableCollectionBase<InvoiceModel>, IInvoice
    {
        public Invoice(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<InvoiceModel>("Invoices"), currentUser)
        {
        }

        public async Task<PagedResult<InvoiceModel>> GetAllAsync(string? category, string? status, int pageNumber, int pageSize)
        {
            var filters = new List<FilterDefinition<InvoiceModel>>();
            if (!string.IsNullOrWhiteSpace(category))
            {
                filters.Add(Builders<InvoiceModel>.Filter.Eq(i => i.Category, category));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add(Builders<InvoiceModel>.Filter.Eq(i => i.Status, status));
            }

            var filter = filters.Count > 0 ? Builders<InvoiceModel>.Filter.And(filters) : FilterDefinition<InvoiceModel>.Empty;

            var totalCount = (int)await Collection.CountDocumentsAsync(filter);
            var items = await Collection.Find(filter)
                .SortByDescending(i => i.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<InvoiceModel>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId)
        {
            return await Collection.Find(i => i.RelatedEnrolmentId == enrolmentId)
                .SortByDescending(i => i.SentAt)
                .ToListAsync();
        }

        public async Task<List<InvoiceModel>> GetByFinancialRecordIdAsync(string financialRecordId)
        {
            return await Collection.Find(i => i.RelatedFinancialRecordId == financialRecordId)
                .SortByDescending(i => i.SentAt)
                .ToListAsync();
        }

        public async Task<InvoiceModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(InvoiceModel invoice)
        {
            await InsertOneAsync(invoice);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, InvoiceModel invoice)
        {
            return await ReplaceOneAsync(i => i.Id == id, invoice);
        }
    }
}
