using ShareService.Models.Course;

namespace ShareService.DataAccess.Interface
{
    public interface ICourse
    {
        Task<bool> CreateCourseAsync(CourseModel course);
        Task<CourseModel?> GetCourseByIdAsync(string id);
        Task<List<CourseModel>> GetAllAsync();
        Task<List<CourseModel>> GetByPartnerIdAsync(string partnerId);
        Task<List<CourseModel>> GetByPartnerIdsAsync(IEnumerable<string> partnerIds);
        Task<bool> UpdateCourseAsync(string id, CourseModel course);
        Task<bool> DeleteCourseAsync(string id);
        Task DeleteByPartnerIdAsync(string partnerId);
    }
}
