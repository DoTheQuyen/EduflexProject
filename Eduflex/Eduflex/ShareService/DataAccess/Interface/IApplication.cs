using MongoDB.Driver;
using ShareService.Common;
using ShareService.Models.Application;
using ShareService.Models.Student;

namespace ShareService.DataAccess.Interface
{
    public interface IApplication
    {
        Task<StudentModel?> GetStudentByUserIdAsync(string userId);
        Task<List<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId);
        Task<PagedResult<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId, PaginationQuery query);
        Task<PagedResult<ApplicationModel>> GetAllApplicationsAsync(PaginationQuery query);
        Task<ApplicationModel?> GetApplicationByIdAsync(string id);
        Task<ApplicationModel> CreateApplicationAsync(ApplicationModel application, IClientSessionHandle? session = null);
        Task<bool> UpdateApplicationStatusAsync(string id, string status, IClientSessionHandle? session = null);
        Task<StudentModel> CreateStudentAsync(StudentModel student, IClientSessionHandle? session = null);
        Task<int> CountApplicationsByStatusAsync(string status);

    }
}
