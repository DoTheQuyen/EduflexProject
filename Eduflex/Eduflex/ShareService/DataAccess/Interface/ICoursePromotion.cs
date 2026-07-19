using ShareService.Models.CoursePromotion;

namespace ShareService.DataAccess.Interface
{
    public interface ICoursePromotion
    {
        Task<CoursePromotionModel> CreateCoursePromotionAsync(CoursePromotionModel promotion);
        Task<CoursePromotionModel?> GetCoursePromotionByIdAsync(string id);
        Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotionsAsync(int count);
        Task<List<CoursePromotionModel>> GetAllCoursePromotionsAsync();
        Task<CoursePromotionModel?> UpdateCoursePromotionAsync(string id, CoursePromotionModel promotion);
        Task<bool> DeleteCoursePromotionAsync(string id);
    }
}
