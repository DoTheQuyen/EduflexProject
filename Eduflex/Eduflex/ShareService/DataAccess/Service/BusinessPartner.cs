using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.BusinessPartner;

namespace ShareService.DataAccess
{
    public class BusinessPartner : AuditableCollectionBase<BusinessPartnerModel>, IBusinessPartner
    {
        public BusinessPartner(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<BusinessPartnerModel>("BusinessPartners"), currentUser)
        {
        }

        public async Task<bool> CreateBusinessPartnerAsync(BusinessPartnerModel partner)
        {
            await InsertOneAsync(partner);
            return true;
        }

        public async Task<BusinessPartnerModel?> GetBusinessPartnerByIdAsync(string id)
        {
            return await Collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<BusinessPartnerModel>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return new List<BusinessPartnerModel>();
            }

            return await Collection.Find(p => idList.Contains(p.Id)).ToListAsync();
        }

        public Task<PagedResult<BusinessPartnerModel>> GetBusinessPartnersAsync(BusinessPartnerFilter filter)
        {
            var mongoFilter = BuildSearchFilter(filter.SearchTerm, p => p.Name, p => p.Email);
            var sort = Builders<BusinessPartnerModel>.Sort.Descending(p => p.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> UpdateBusinessPartnerAsync(string id, BusinessPartnerModel partner)
        {
            return await ReplaceOneAsync(p => p.Id == id, partner);
        }

        public async Task<bool> DeleteBusinessPartnerAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
