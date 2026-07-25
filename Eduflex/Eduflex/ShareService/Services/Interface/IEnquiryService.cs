using ShareService.Common;
using ShareService.Models.Enquiry;

namespace ShareService.Services.Interface
{
    public interface IEnquiryService
    {
        Task<bool> CreateEnquiry(EnquiryModel enquiry);
        Task<PagedResult<EnquiryModel>> GetEnquiries(EnquiryFilter filter);
        Task<EnquiryModel?> GetEnquiryAsync(string id);
        Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel updateModel);
        Task<bool> DeleteEnquiriesAsync(string id);
    }
}
