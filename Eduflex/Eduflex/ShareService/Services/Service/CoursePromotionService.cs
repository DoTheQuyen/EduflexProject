using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.CoursePromotion;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class CoursePromotionService : ICoursePromotionService
    {
        private readonly ICoursePromotion _coursePromotionDataAccess;
        private readonly IValidator<CoursePromotionModel> _createCoursePromotionValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<CoursePromotionService> _logger;

        public CoursePromotionService(
            ICoursePromotion coursePromotionDataAccess,
            IValidator<CoursePromotionModel> createCoursePromotionValidator,
            IPermissionService permissionService,
            ILogger<CoursePromotionService> logger)
        {
            _coursePromotionDataAccess = coursePromotionDataAccess;
            _createCoursePromotionValidator = createCoursePromotionValidator;
            _permissionService = permissionService;
            _logger = logger;
        }

        // Auth: requires CoursePromotionsAdd permission (staff-only).
        public async Task<bool> CreateCoursePromotion(CoursePromotionModel promotion, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.CoursePromotionsAdd.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to create course promotions");
            }

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

        // Auth: none — deliberately public/anonymous, feeds the marketing site carousel.
        public async Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotions(int count)
        {
            return await _coursePromotionDataAccess.GetFeaturedActiveCoursePromotionsAsync(count);
        }

        // Auth: requires CoursePromotionsView permission (staff-only).
        public async Task<PagedResult<CoursePromotionModel>> GetCoursePromotions(CoursePromotionFilter filter, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.CoursePromotionsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view course promotions");
            }

            return await _coursePromotionDataAccess.GetCoursePromotionsAsync(filter);
        }

        // Auth: requires CoursePromotionsEdit permission (staff-only).
        public async Task<bool> UpdateCoursePromotion(string id, CoursePromotionModel promotion, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.CoursePromotionsEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to update course promotions");
            }

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

        // Auth: requires CoursePromotionsDelete permission (staff-only).
        public async Task<bool> DeleteCoursePromotion(string id, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.CoursePromotionsDelete.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to delete course promotions");
            }

            var deleted = await _coursePromotionDataAccess.DeleteCoursePromotionAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted course promotion with ID: {CoursePromotionId}", id);
            }
            return deleted;
        }
    }
}
