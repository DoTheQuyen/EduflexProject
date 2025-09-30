using ShareService.Models;

namespace ShareService.DataAccess.Interface
{
    public interface IApplication
    {
        Task<StudentModel?> GetStudentByUserIdAsync(string userId);
        Task<List<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId);
        Task<ApplicationModel?> GetApplicationByIdAsync(string id);
        Task<ApplicationModel> CreateApplicationAsync(ApplicationModel application);
        Task<bool> UpdateApplicationStatusAsync(string id, string status);

        Task DebugDatabaseContent(string userId);
    }
}