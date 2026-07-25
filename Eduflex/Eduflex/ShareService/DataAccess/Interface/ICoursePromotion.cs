using ShareService.Common;
using ShareService.Models.CoursePromotion;

namespace ShareService.DataAccess.Interface
{
    public interface ICoursePromotion
    {
        Task<bool> CreateCoursePromotionAsync(CoursePromotionModel promotion);
        Task<CoursePromotionModel?> GetCoursePromotionByIdAsync(string id);
        Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotionsAsync(int count);
        Task<PagedResult<CoursePromotionModel>> GetCoursePromotionsAsync(CoursePromotionFilter filter);
        Task<bool> UpdateCoursePromotionAsync(string id, CoursePromotionModel promotion);
        Task<bool> DeleteCoursePromotionAsync(string id);
    }
}
