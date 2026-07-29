using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.EducationPartner;

namespace ShareService.DataAccess
{
    public class EducationPartner : AuditableCollectionBase<EducationPartnerModel>, IEducationPartner
    {
        public EducationPartner(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<EducationPartnerModel>("EducationPartners"), currentUser)
        {
        }

        public async Task<bool> CreateEducationPartnerAsync(EducationPartnerModel partner)
        {
            await InsertOneAsync(partner);
            return true;
        }

        public async Task<EducationPartnerModel?> GetEducationPartnerByIdAsync(string id)
        {
            return await Collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<EducationPartnerModel>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return new List<EducationPartnerModel>();
            }

            return await Collection.Find(p => idList.Contains(p.Id)).ToListAsync();
        }

        public Task<PagedResult<EducationPartnerModel>> GetEducationPartnersAsync(EducationPartnerFilter filter)
        {
            var mongoFilter = BuildSearchFilter(filter.SearchTerm, p => p.Name, p => p.Location, p => p.Trademark);
            var sort = Builders<EducationPartnerModel>.Sort.Descending(p => p.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> UpdateEducationPartnerAsync(string id, EducationPartnerModel partner)
        {
            return await ReplaceOneAsync(p => p.Id == id, partner);
        }

        public async Task<bool> DeleteEducationPartnerAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsWithBusinessPartnerIdAsync(string businessPartnerId)
        {
            return await Collection.Find(p => p.BusinessPartnerId == businessPartnerId).AnyAsync();
        }
    }
}
