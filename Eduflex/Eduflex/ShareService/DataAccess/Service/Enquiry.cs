using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enquiry;

namespace ShareService.DataAccess
{
    public class Enquiry : AuditableCollectionBase<EnquiryModel>, IEnquiry
    {
        public Enquiry(IMongoDatabase database, ICurrentUserService currentUser)
         : base(database.GetCollection<EnquiryModel>("Enquiries"), currentUser)
        {
        }

        public async Task<bool> CreateEnquiryAsync(EnquiryModel enquiry)
        {
            await InsertOneAsync(enquiry);
            return true;
        }

        public async Task<EnquiryModel?> GetEnquiryAsync(string id)
        {
            return await Collection
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<EnquiryModel?> GetEnquiryAsync(string? email, string? mobile)
        {
            return await Collection
                .Find(u => u.Email == email || u.Mobile == mobile)
                .FirstOrDefaultAsync();
        }

        public Task<PagedResult<EnquiryModel>> GetEnquiriesAsync(EnquiryFilter filter)
        {
            var filters = new List<FilterDefinition<EnquiryModel>>
            {
                BuildSearchFilter(filter.SearchTerm, e => e.FirstName, e => e.LastName, e => e.Email, e => e.Mobile)
            };

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var statusStrings = filter.Statuses.Select(s => s.ToString()).ToList();
                filters.Add(Builders<EnquiryModel>.Filter.In(e => e.Status, statusStrings));
            }

            var mongoFilter = Builders<EnquiryModel>.Filter.And(filters);
            var sort = Builders<EnquiryModel>.Sort.Descending(e => e.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel enquiry)
        {
            return await ReplaceOneAsync(p => p.Id == id, enquiry);
        }

        public async Task<bool> DeleteEnquiriesAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
