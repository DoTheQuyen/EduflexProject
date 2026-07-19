using ShareService.Models.CoursePromotion;

namespace ShareService.Services.Interface
{
    public interface ICoursePromotionService
    {
        Task<CoursePromotionModel> CreateCoursePromotion(CreateCoursePromotionModel createDto);
        Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count);
        Task<List<CoursePromotionModel>> GetAllCoursePromotions();
        Task<CoursePromotionModel?> UpdateCoursePromotion(string id, CreateCoursePromotionModel updateDto);
        Task<bool> DeleteCoursePromotion(string id);
    }
}
