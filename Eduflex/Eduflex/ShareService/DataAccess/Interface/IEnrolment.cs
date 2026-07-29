using MongoDB.Driver;
using ShareService.Common;
using ShareService.Models.Enrolment;

namespace ShareService.DataAccess.Interface
{
    public interface IEnrolment
    {
        Task<bool> CreateEnrolmentAsync(EnrolmentModel enrolment, IClientSessionHandle? session = null);
        Task<EnrolmentModel?> GetEnrolmentAsync(string id);
        Task<EnrolmentModel?> GetEnrolmentByEnquiryIdAsync(string enquiryId);
        Task<List<EnrolmentModel>> GetByIdsAsync(IEnumerable<string> ids);
        Task<PagedResult<EnrolmentModel>> GetEnrolmentsAsync(EnrolmentFilter filter);
        Task<bool> ReplaceEnrolmentAsync(string id, EnrolmentModel enrolment);
        Task<bool> DeleteEnrolmentAsync(string id);
    }
}
