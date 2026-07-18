using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models.CoursePromotion;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class CoursePromotionService : ICoursePromotionService
    {
        private readonly ICoursePromotion _coursePromotionDataAccess;
        private readonly IValidator<CreateCoursePromotionModel> _createCoursePromotionValidator;
        private readonly ILogger<CoursePromotionService> _logger;

        public CoursePromotionService(
            ICoursePromotion coursePromotionDataAccess,
            IValidator<CreateCoursePromotionModel> createCoursePromotionValidator,
            ILogger<CoursePromotionService> logger)
        {
            _coursePromotionDataAccess = coursePromotionDataAccess;
            _createCoursePromotionValidator = createCoursePromotionValidator;
            _logger = logger;
        }

        public async Task<CoursePromotionModel> CreateCoursePromotion(CreateCoursePromotionModel createDto)
        {
            var validate = await _createCoursePromotionValidator.ValidateAsync(createDto);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for course promotion creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var promotion = new CoursePromotionModel
            {
                CourseName = createDto.CourseName,
                UniversityName = createDto.UniversityName,
                Semester = createDto.Semester,
                ScholarshipLabel = createDto.ScholarshipLabel,
                Location = createDto.Location,
                Tuition = createDto.Tuition,
                Opportunities = createDto.Opportunities,
                ExpiryDate = createDto.ExpiryDate,
                Note = createDto.Note,
                WebsiteUrl = createDto.WebsiteUrl,
                IsFeatured = createDto.IsFeatured,
                DisplayOrder = createDto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _coursePromotionDataAccess.CreateCoursePromotionAsync(promotion);
            _logger.LogInformation("Created new course promotion with ID: {CoursePromotionId} for {CourseName}", created.Id, created.CourseName);
            return created;
        }

        public async Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count)
        {
            return await _coursePromotionDataAccess.GetFeaturedActiveCoursePromotionsAsync(count);
        }

        public async Task<List<CoursePromotionModel>> GetAllCoursePromotions()
        {
            return await _coursePromotionDataAccess.GetAllCoursePromotionsAsync();
        }

        public async Task<CoursePromotionModel?> UpdateCoursePromotion(string id, CreateCoursePromotionModel updateDto)
        {
            var validate = await _createCoursePromotionValidator.ValidateAsync(updateDto);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for course promotion update: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _coursePromotionDataAccess.GetCoursePromotionByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            var promotion = new CoursePromotionModel
            {
                Id = id,
                CourseName = updateDto.CourseName,
                UniversityName = updateDto.UniversityName,
                Semester = updateDto.Semester,
                ScholarshipLabel = updateDto.ScholarshipLabel,
                Location = updateDto.Location,
                Tuition = updateDto.Tuition,
                Opportunities = updateDto.Opportunities,
                ExpiryDate = updateDto.ExpiryDate,
                Note = updateDto.Note,
                WebsiteUrl = updateDto.WebsiteUrl,
                IsFeatured = updateDto.IsFeatured,
                DisplayOrder = updateDto.DisplayOrder,
                CreatedAt = existing.CreatedAt
            };

            var updated = await _coursePromotionDataAccess.UpdateCoursePromotionAsync(id, promotion);
            if (updated != null)
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
