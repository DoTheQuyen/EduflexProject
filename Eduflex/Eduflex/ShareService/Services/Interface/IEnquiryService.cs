using ShareService.Enums.Roles;
using ShareService.Models.Enquiry;

namespace ShareService.Services.Interface
{
    public interface IEnquiryService
    {
        Task<bool> CreateEnquiry(EnquiryModel enquiry);
        Task<List<EnquiryModel>> GetAllEnquiriesAsync(EnquiryEnums? status);
        Task<EnquiryModel?> GetEnquiryAsync(string id);
        Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel updateModel);
        Task<bool> DeleteEnquiriesAsync(string id);
    }
}
