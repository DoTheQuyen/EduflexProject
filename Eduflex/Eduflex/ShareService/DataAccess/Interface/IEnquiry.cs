using ShareService.Common;
using ShareService.Models.Enquiry;

namespace ShareService.DataAccess.Interface
{
    public interface IEnquiry
    {
        Task<bool> CreateEnquiryAsync(EnquiryModel enquiry);
        Task<EnquiryModel?> GetEnquiryAsync(string id);
        Task<EnquiryModel?> GetEnquiryAsync(string? email, string? mobile);
        Task<PagedResult<EnquiryModel>> GetEnquiriesAsync(EnquiryFilter filter);
        Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel enquiry);
        Task<bool> DeleteEnquiriesAsync(string id);
        Task<Dictionary<string, int>> GetMonthlyCountsAsync(DateTime since);
        Task<Dictionary<string, int>> GetStatusCountsAsync();
    }
}
