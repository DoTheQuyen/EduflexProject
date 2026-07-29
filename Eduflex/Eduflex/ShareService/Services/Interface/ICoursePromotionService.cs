using ShareService.Common;
using ShareService.Models.CoursePromotion;

namespace ShareService.Services.Interface
{
    public interface ICoursePromotionService
    {
        Task<bool> CreateCoursePromotion(CoursePromotionModel promotion, string userId);
        Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count);
        Task<PagedResult<CoursePromotionModel>> GetCoursePromotions(CoursePromotionFilter filter, string userId);
        Task<bool> UpdateCoursePromotion(string id, CoursePromotionModel promotion, string userId);
        Task<bool> DeleteCoursePromotion(string id, string userId);
    }
}
