using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.VisaProcess;

namespace ShareService.DataAccess
{
    public class VisaProcessTemplate : AuditableCollectionBase<VisaProcessTemplateModel>, IVisaProcessTemplate
    {
        public VisaProcessTemplate(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<VisaProcessTemplateModel>("VisaProcessTemplates"), currentUser)
        {
        }

        public async Task<List<VisaProcessTemplateModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<VisaProcessTemplateModel>.Empty)
                .SortBy(t => t.Country).ThenBy(t => t.Category).ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<VisaProcessTemplateModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(VisaProcessTemplateModel template)
        {
            await InsertOneAsync(template);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, VisaProcessTemplateModel template)
        {
            return await ReplaceOneAsync(t => t.Id == id, template);
        }
    }
}
