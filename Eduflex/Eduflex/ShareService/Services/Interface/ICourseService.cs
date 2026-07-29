using ShareService.Common;
using ShareService.Models.Course;

namespace ShareService.Services.Interface
{
    public interface ICourseService
    {
        Task<bool> CreateCourse(CourseModel course, string userId);
        Task<bool> UpdateCourse(string id, CourseModel course, string userId);
        Task<bool> DeleteCourse(string id, string userId);
        Task<List<CourseModel>> GetCoursesByPartnerId(string partnerId, string userId);
        Task<Dictionary<string, List<CourseModel>>> GetCoursesByPartnerIds(IEnumerable<string> partnerIds);
        Task<PagedResult<CourseSearchResult>> SearchCourses(CourseSearchFilter filter, string userId);
    }
}
