using ShareService.Common;
using ShareService.Models.Student;

namespace ShareService.DataAccess.Interface
{
    public interface IStudentDB
    {
        Task<StudentModel?> GetByIdAsync(string id);
        Task<StudentModel?> GetByUserIdAsync(string userId);
        Task<StudentModel?> GetByEmailAsync(string email);
        Task<StudentModel?> GetByPassportNumberAsync(string passportNumber);
        Task<StudentModel?> GetByDateOfBirthAsync(DateTime dateOfBirth);
        Task<StudentModel> CreateAsync(StudentModel student);
        Task<bool> UpdateAsync(string id, StudentModel student);
        Task<PagedResult<StudentModel>> SearchAsync(StudentFilter filter, List<string>? restrictToUserIds);
    }
}
