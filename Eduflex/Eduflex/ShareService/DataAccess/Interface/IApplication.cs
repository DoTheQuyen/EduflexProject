using MongoDB.Driver;
using ShareService.Models;

namespace ShareService.DataAccess.Interface
{
    public interface IApplication
    {
        Task<StudentModel?> GetStudentByUserIdAsync(string userId);
        Task<List<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId);
        Task<ApplicationModel?> GetApplicationByIdAsync(string id);
        Task<ApplicationModel> CreateApplicationAsync(ApplicationModel application, IClientSessionHandle? session = null);
        Task<bool> UpdateApplicationStatusAsync(string id, string status, IClientSessionHandle? session = null);

    }
}