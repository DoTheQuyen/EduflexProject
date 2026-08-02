using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.DynamicForm;

namespace ShareService.DataAccess
{
    public class DynamicFormTemplate : AuditableCollectionBase<DynamicFormTemplateModel>, IDynamicFormTemplate
    {
        public DynamicFormTemplate(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<DynamicFormTemplateModel>("DynamicFormTemplates"), currentUser)
        {
        }

        public async Task<List<DynamicFormTemplateModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<DynamicFormTemplateModel>.Empty)
                .SortBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<DynamicFormTemplateModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(DynamicFormTemplateModel template)
        {
            await InsertOneAsync(template);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, DynamicFormTemplateModel template)
        {
            return await ReplaceOneAsync(t => t.Id == id, template);
        }
    }
}
