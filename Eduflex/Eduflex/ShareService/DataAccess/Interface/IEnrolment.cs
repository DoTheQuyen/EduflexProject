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
        Task<EnrolmentModel?> GetEnrolmentByStudentApplicationIdAsync(string studentApplicationId);
        Task<List<EnrolmentModel>> GetByIdsAsync(IEnumerable<string> ids);
        Task<List<EnrolmentModel>> GetByStudentUserIdAsync(string studentUserId);
        Task<PagedResult<EnrolmentModel>> GetEnrolmentsAsync(EnrolmentFilter filter);
        Task<bool> ReplaceEnrolmentAsync(string id, EnrolmentModel enrolment);
        Task<bool> DeleteEnrolmentAsync(string id);
        Task<Dictionary<string, int>> GetMonthlyCountsAsync(DateTime since);
        Task<Dictionary<string, int>> GetStatusCountsAsync();
    }
}
