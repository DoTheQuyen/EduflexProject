using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.VisaProcess;

namespace ShareService.DataAccess
{
    public class PractitionerTag : AuditableCollectionBase<PractitionerTagModel>, IPractitionerTag
    {
        public PractitionerTag(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<PractitionerTagModel>("PractitionerTags"), currentUser)
        {
        }

        public async Task<List<PractitionerTagModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<PractitionerTagModel>.Empty)
                .SortBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<PractitionerTagModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(PractitionerTagModel tag)
        {
            await InsertOneAsync(tag);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, PractitionerTagModel tag)
        {
            return await ReplaceOneAsync(t => t.Id == id, tag);
        }
    }
}
