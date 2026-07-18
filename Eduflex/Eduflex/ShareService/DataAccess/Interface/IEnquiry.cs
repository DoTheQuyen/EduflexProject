using ShareService.Models.Enquiry;

namespace ShareService.DataAccess.Interface
{
    public interface IEnquiry
    {
        Task<EnquiryModel> CreateEnquiryAsync(EnquiryModel enquiry);
    }
}
