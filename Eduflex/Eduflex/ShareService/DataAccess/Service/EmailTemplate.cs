using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enrolment;

namespace ShareService.DataAccess
{
    public class EmailTemplate : AuditableCollectionBase<EmailTemplateModel>, IEmailTemplate
    {
        public EmailTemplate(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<EmailTemplateModel>("EmailTemplates"), currentUser)
        {
        }

        public async Task<List<EmailTemplateModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<EmailTemplateModel>.Empty)
                .SortBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<EmailTemplateModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<EmailTemplateModel?> GetByKeyAsync(string key)
        {
            return await Collection.Find(t => t.Key == key).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(EmailTemplateModel template)
        {
            await InsertOneAsync(template);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, EmailTemplateModel template)
        {
            return await ReplaceOneAsync(t => t.Id == id, template);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(t => t.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
