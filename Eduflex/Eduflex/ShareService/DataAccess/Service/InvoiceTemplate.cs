using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Invoice;

namespace ShareService.DataAccess
{
    public class InvoiceTemplate : AuditableCollectionBase<InvoiceTemplateModel>, IInvoiceTemplate
    {
        public InvoiceTemplate(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<InvoiceTemplateModel>("InvoiceTemplates"), currentUser)
        {
        }

        public async Task<List<InvoiceTemplateModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<InvoiceTemplateModel>.Empty)
                .SortBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<InvoiceTemplateModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(InvoiceTemplateModel template)
        {
            await InsertOneAsync(template);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, InvoiceTemplateModel template)
        {
            return await ReplaceOneAsync(t => t.Id == id, template);
        }

        public async Task<int?> ReserveNextSequenceAsync(string templateId)
        {
            var update = Builders<InvoiceTemplateModel>.Update.Inc(t => t.NextSequence, 1);
            var options = new FindOneAndUpdateOptions<InvoiceTemplateModel> { ReturnDocument = ReturnDocument.Before };

            var before = await FindOneAndUpdateAsync(t => t.Id == templateId, update, options);
            return before == null ? null : before.NextSequence;
        }
    }
}
