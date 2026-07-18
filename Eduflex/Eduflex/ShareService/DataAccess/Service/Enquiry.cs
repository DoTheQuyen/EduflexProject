using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enquiry;

namespace ShareService.DataAccess
{
    public class Enquiry : IEnquiry
    {
        private readonly IMongoCollection<EnquiryModel> _enquiriesCollection;

        public Enquiry(IMongoDatabase database)
        {
            _enquiriesCollection = database.GetCollection<EnquiryModel>("Enquiries");
        }

        public async Task<EnquiryModel> CreateEnquiryAsync(EnquiryModel enquiry)
        {
            await _enquiriesCollection.InsertOneAsync(enquiry);
            return enquiry;
        }
    }
}
