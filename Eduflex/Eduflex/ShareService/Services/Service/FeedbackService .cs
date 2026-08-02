using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Feedback;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class FeedbackService : IFeedbackService
    {
        private const string LatestFeedbackCacheKey = "feedback:latest";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly IFeedback _feedbackDataAccess;
        private readonly IValidator<FeedbackModel> _createFeedbackValidator;
        private readonly IPermissionService _permissionService;
        private readonly ISettingsService _settingsService;
        private readonly IDistributedCache _cache;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            IFeedback feedbackDataAccess,
            IValidator<FeedbackModel> createFeedbackValidator,
            IPermissionService permissionService,
            ISettingsService settingsService,
            IDistributedCache cache,
            INotificationPublisher notificationPublisher,
            ILogger<FeedbackService> logger)
        {
            _feedbackDataAccess = feedbackDataAccess;
            _createFeedbackValidator = createFeedbackValidator;
            _permissionService = permissionService;
            _settingsService = settingsService;
            _cache = cache;
            _notificationPublisher = notificationPublisher;
            _logger = logger;
        }

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
            if (created)
            {
                await _cache.RemoveAsync(LatestFeedbackCacheKey);
                await _notificationPublisher.PublishToRoleAsync(
                    module: "Feedback",
                    entityId: feedback.Id,
                    summary: $"New feedback from {feedback.Name}",
                    role: SystemRole.Staff);
            }
            _logger.LogInformation("Created new feedback with ID: {FeedbackId} for {Name}", feedback.Id, feedback.Name);
            return created;
        }

        public async Task<List<FeedbackModel>> GetLatestFeedback(int count)
        {
            var cached = await _cache.GetAsync<List<FeedbackModel>>(LatestFeedbackCacheKey);

            if (cached == null || cached.Count < count)
            {
                var settings = await _settingsService.GetSettingsAsync();
                var cacheBatchSize = Math.Max(count, settings.FeedbackDefaultLatestCount);

                cached = await _feedbackDataAccess.GetLatestFeedbackAsync(cacheBatchSize);
                await _cache.SetAsync(LatestFeedbackCacheKey, cached, CacheDuration);
            }

            return cached.Take(count).ToList();
        }

        public async Task<PagedResult<FeedbackModel>> GetFeedback(PaginationQuery query, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.FeedbackView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view feedback");
            }

            return await _feedbackDataAccess.GetFeedbackAsync(query);
        }

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
                await _cache.RemoveAsync(LatestFeedbackCacheKey);
                _logger.LogInformation("Deleted feedback with ID: {FeedbackId}", id);
            }
            return deleted;
        }
    }
}