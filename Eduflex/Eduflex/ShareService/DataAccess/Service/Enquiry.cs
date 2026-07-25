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

        public async Task<List<EnquiryModel>> GetAllEnquiriesAsync(string? status)
        {
            var filter = string.IsNullOrEmpty(status)
                ? FilterDefinition<EnquiryModel>.Empty
                : Builders<EnquiryModel>.Filter.Eq(e => e.Status, status);

            return await Collection
                .Find(filter)
                .ToListAsync();
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
