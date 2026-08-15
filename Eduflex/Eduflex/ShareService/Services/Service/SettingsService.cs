using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Settings;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettings _settingsDataAccess;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(
            ISettings settingsDataAccess,
            IPermissionService permissionService,
            ILogger<SettingsService> logger)
        {
            _settingsDataAccess = settingsDataAccess;
            _permissionService = permissionService;
            _logger = logger;
        }

        // Auth: none beyond [Authorize] on the controller — every logged-in user
        // (including students filling out the application form) needs to read the
        // effective document-upload limits, not just staff.
        public async Task<SettingsModel> GetSettingsAsync()
        {
            var settings = await _settingsDataAccess.GetSettingsAsync();
            if (settings == null)
            {
                throw new KeyNotFoundException("Settings have not been initialized. Run the DBMigration project.");
            }

            return settings;
        }

        // Auth: requires SettingsEdit permission (Admin-only — this is app-wide config).
        public async Task<SettingsModel> UpdateSettingsAsync(SettingsModel settings, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.SettingsEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to update settings");
            }

            if (settings.FeedbackDefaultLatestCount <= 0)
            {
                throw new ArgumentException("FeedbackDefaultLatestCount must be greater than zero");
            }
            if (settings.CoursePromotionDefaultLatestCount <= 0)
            {
                throw new ArgumentException("CoursePromotionDefaultLatestCount must be greater than zero");
            }
            if (settings.MaxApplicationsPerStudent <= 0)
            {
                throw new ArgumentException("MaxApplicationsPerStudent must be greater than zero");
            }
            ValidateUploadLimit(settings.DocumentUpload.Default, "Default");
            ValidateUploadLimit(settings.DocumentUpload.Other, "Other");
            ValidateUploadLimit(settings.ImageUpload, "Image");
            ValidateUploadLimit(settings.ContractUpload, "Contract");
            ValidateUploadLimit(settings.EnrolmentUpload, "Enrolment");
            if (string.IsNullOrWhiteSpace(settings.ChatSystemPrompt))
            {
                throw new ArgumentException("ChatSystemPrompt cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatApiUrl))
            {
                throw new ArgumentException("ChatApiUrl cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatGroqApiUrl))
            {
                throw new ArgumentException("ChatGroqApiUrl cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatOpenRouterApiUrl))
            {
                throw new ArgumentException("ChatOpenRouterApiUrl cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatGeminiModel))
            {
                throw new ArgumentException("ChatGeminiModel cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatGroqModel))
            {
                throw new ArgumentException("ChatGroqModel cannot be empty");
            }
            if (string.IsNullOrWhiteSpace(settings.ChatOpenRouterModel))
            {
                throw new ArgumentException("ChatOpenRouterModel cannot be empty");
            }

            var updated = await _settingsDataAccess.UpsertSettingsAsync(settings);
            _logger.LogInformation("Settings updated by {UserId}", userId);
            return updated;
        }

        private static void ValidateUploadLimit(UploadLimit limit, string label)
        {
            if (limit.MaxSizeMB <= 0)
            {
                throw new ArgumentException($"{label} upload MaxSizeMB must be greater than zero");
            }
            if (limit.MaxFileCount <= 0)
            {
                throw new ArgumentException($"{label} upload MaxFileCount must be greater than zero");
            }
        }
    }
}
