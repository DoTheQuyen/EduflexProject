using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.CoursePromotion;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class CoursePromotionService : ICoursePromotionService
    {
        private readonly ICoursePromotion _coursePromotionDataAccess;
        private readonly IValidator<CoursePromotionModel> _createCoursePromotionValidator;
        private readonly ILogger<CoursePromotionService> _logger;

        public CoursePromotionService(
            ICoursePromotion coursePromotionDataAccess,
            IValidator<CoursePromotionModel> createCoursePromotionValidator,
            ILogger<CoursePromotionService> logger)
        {
            _coursePromotionDataAccess = coursePromotionDataAccess;
            _createCoursePromotionValidator = createCoursePromotionValidator;
            _logger = logger;
        }

        public async Task<bool> CreateCoursePromotion(CoursePromotionModel promotion)
        {
            var validate = await _createCoursePromotionValidator.ValidateAsync(promotion);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for course promotion creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            promotion.Id = string.Empty;
            promotion.CreatedAt = DateTime.UtcNow;

            var created = await _coursePromotionDataAccess.CreateCoursePromotionAsync(promotion);
            _logger.LogInformation("Created new course promotion with ID: {CoursePromotionId} for {CourseName}", promotion.Id, promotion.CourseName);
            return created;
        }

        public async Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count)
        {
            return await _coursePromotionDataAccess.GetFeaturedActiveCoursePromotionsAsync(count);
        }

        public async Task<PagedResult<CoursePromotionModel>> GetCoursePromotions(PaginationQuery query, bool? isFeatured)
        {
            return await _coursePromotionDataAccess.GetCoursePromotionsAsync(query, isFeatured);
        }

        public async Task<bool> UpdateCoursePromotion(string id, CoursePromotionModel promotion)
        {
            var validate = await _createCoursePromotionValidator.ValidateAsync(promotion);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for course promotion update: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _coursePromotionDataAccess.GetCoursePromotionByIdAsync(id);
            if (existing == null)
            {
                throw new ArgumentException("Course promotion not found");
            }

            existing.ApplyEditableFields(promotion);

            var updated = await _coursePromotionDataAccess.UpdateCoursePromotionAsync(id, existing);
            if (updated)
            {
                _logger.LogInformation("Updated course promotion with ID: {CoursePromotionId}", id);
            }
            return updated;
        }

        public async Task<bool> DeleteCoursePromotion(string id)
        {
            var deleted = await _coursePromotionDataAccess.DeleteCoursePromotionAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted course promotion with ID: {CoursePromotionId}", id);
            }
            return deleted;
        }
    }
}
