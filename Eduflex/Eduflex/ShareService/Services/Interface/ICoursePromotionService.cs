using ShareService.Common;
using ShareService.Models.CoursePromotion;

namespace ShareService.Services.Interface
{
    public interface ICoursePromotionService
    {
        Task<bool> CreateCoursePromotion(CoursePromotionModel promotion);
        Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count);
        Task<PagedResult<CoursePromotionModel>> GetCoursePromotions(PaginationQuery query, bool? isFeatured);
        Task<bool> UpdateCoursePromotion(string id, CoursePromotionModel promotion);
        Task<bool> DeleteCoursePromotion(string id);
    }
}
