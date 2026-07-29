using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Feedback;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedback _feedbackDataAccess;
        private readonly IValidator<FeedbackModel> _createFeedbackValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            IFeedback feedbackDataAccess,
            IValidator<FeedbackModel> createFeedbackValidator,
            IPermissionService permissionService,
            ILogger<FeedbackService> logger)
        {
            _feedbackDataAccess = feedbackDataAccess;
            _createFeedbackValidator = createFeedbackValidator;
            _permissionService = permissionService;
            _logger = logger;
        }

        // CreateFeedback intentionally has no permission check — any authenticated user
        // (including students) may submit feedback about their own experience, gated only
        // by [Authorize] on the controller. View/Delete below are staff moderation actions.
        public async Task<bool> CreateFeedback(FeedbackModel feedback)
        {
            var validate = await _createFeedbackValidator.ValidateAsync(feedback);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for feedback creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            feedback.Id = string.Empty;
            feedback.CreatedAt = DateTime.UtcNow;

            var created = await _feedbackDataAccess.CreateFeedbackAsync(feedback);
            _logger.LogInformation("Created new feedback with ID: {FeedbackId} for {Name}", feedback.Id, feedback.Name);
            return created;
        }

        // Auth: none — deliberately public/anonymous, feeds the marketing site carousel.
        public async Task<List<FeedbackModel>> GetLatestFeedback(int count)
        {
            return await _feedbackDataAccess.GetLatestFeedbackAsync(count);
        }

        // Auth: requires FeedbackView permission (staff-only moderation action).
        public async Task<PagedResult<FeedbackModel>> GetFeedback(PaginationQuery query, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.FeedbackView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view feedback");
            }

            return await _feedbackDataAccess.GetFeedbackAsync(query);
        }

        // Auth: requires FeedbackDelete permission (staff-only moderation action).
        public async Task<bool> DeleteFeedback(string id, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.FeedbackDelete.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to delete feedback");
            }

            var deleted = await _feedbackDataAccess.DeleteFeedbackAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted feedback with ID: {FeedbackId}", id);
            }
            return deleted;
        }
    }
}
