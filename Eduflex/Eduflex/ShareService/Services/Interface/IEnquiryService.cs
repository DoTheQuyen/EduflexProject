using ShareService.Models.Enquiry;

namespace ShareService.Services.Interface
{
    public interface IEnquiryService
    {
        Task<EnquiryModel> CreateEnquiry(CreateEnquiryModel createDto);
    }
}
