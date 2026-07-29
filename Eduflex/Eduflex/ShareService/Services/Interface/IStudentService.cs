using ShareService.Common;
using ShareService.Models.Auth;
using ShareService.Models.Student;

namespace ShareService.Services.Interface
{
    public interface IStudentService
    {
        Task<StudentModel?> GetMyProfileAsync(string userId);

        Task<PagedResult<StudentAccountModel>> SearchStudentsAsync(StudentFilter filter, string actingUserId);
        Task<StudentAccountModel?> GetStudentAsync(string id, string actingUserId);
        Task<DuplicateCheckResult> CheckDuplicateAsync(string email, string mobile, DateTime dateOfBirth, string passportNumber, string actingUserId);
        Task<StudentAccountModel> CreateStudentAsync(UserModel newUser, StudentModel studentProfile, string actingUserId);
        Task<StudentAccountModel> UpdateStudentAsync(string id, string email, string mobile, StudentModel profileUpdates, string actingUserId);
        Task<bool> DeactivateStudentAsync(string id, string actingUserId);
        Task<bool> ReactivateStudentAsync(string id, string actingUserId);
        Task SendPasswordResetAsync(string id, string actingUserId);
    }
}
