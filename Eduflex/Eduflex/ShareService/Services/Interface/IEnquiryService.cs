using ShareService.Common;
using ShareService.Models.Enquiry;

namespace ShareService.Services.Interface
{
    public interface IEnquiryService
    {
        Task<bool> CreateEnquiry(EnquiryModel enquiry);
        Task<PagedResult<EnquiryModel>> GetEnquiries(EnquiryFilter filter, string userId);
        Task<EnquiryModel?> GetEnquiryAsync(string id, string userId);
        Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel updateModel, string userId);
        Task<bool> DeleteEnquiriesAsync(string id, string userId);
        Task<Dictionary<string, int>> GetMonthlyCountsAsync(string userId, DateTime since);
        Task<Dictionary<string, int>> GetStatusCountsAsync(string userId);
    }
}
