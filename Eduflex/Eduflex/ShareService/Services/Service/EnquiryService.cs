using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Mapping;
using ShareService.Models.Auth;
using ShareService.Models.Enquiry;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class EnquiryService : IEnquiryService
    {
        private readonly IEnquiry _enquiryDataAccess;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IValidator<EnquiryModel> _createEnquiryValidator;
        private readonly IPermissionService _permissionService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger<EnquiryService> _logger;

        public EnquiryService(
            IEnquiry enquiryDataAccess,
            IRecaptchaService recaptchaService,
            IValidator<EnquiryModel> createEnquiryValidator,
            IPermissionService permissionService,
            INotificationPublisher notificationPublisher,
            ILogger<EnquiryService> logger)
        {
            _enquiryDataAccess = enquiryDataAccess;
            _recaptchaService = recaptchaService;
            _createEnquiryValidator = createEnquiryValidator;
            _permissionService = permissionService;
            _notificationPublisher = notificationPublisher;
            _logger = logger;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        public async Task<bool> CreateEnquiry(EnquiryModel enquiry)
        {
            var validate = await _createEnquiryValidator.ValidateAsync(enquiry, options => options.IncludeRuleSets("default", "Create"));
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for enquiry creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var isHuman = await _recaptchaService.VerifyAsync(enquiry.RecaptchaToken);
            if (!isHuman)
            {
                _logger.LogWarning("reCAPTCHA verification failed for enquiry from {Email}", enquiry.Email);
                throw new ArgumentException("reCAPTCHA verification failed. Please try again.");
            }

            var existingEnquiry = await _enquiryDataAccess.GetEnquiryAsync(enquiry.Email, enquiry.Mobile);
            if (existingEnquiry != null && existingEnquiry.Status == EnquiryEnums.New.ToString())
            {
                throw new ArgumentException("Your enquiry is sent to our staff. We will contact you shortly.");
            }

            try
            {
                enquiry.Id = string.Empty;
                enquiry.Status = EnquiryEnums.New.ToString();
                enquiry.CreatedAt = DateTime.UtcNow;

                var created = await _enquiryDataAccess.CreateEnquiryAsync(enquiry);

                if (created)
                {
                    await _notificationPublisher.PublishToRoleAsync(
                        module: "Enquiry",
                        entityId: enquiry.Id,
                        summary: $"New enquiry from {enquiry.FirstName} {enquiry.LastName}",
                        role: SystemRole.Staff);
                }

                _logger.LogInformation("Created new enquiry with ID: {EnquiryId} from {Email}",
                    enquiry.Id, enquiry.Email);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateEnquiry for {Email}", enquiry.Email);
                throw new Exception("Error creating enquiry", ex);
            }
        }

        public async Task<PagedResult<EnquiryModel>> GetEnquiries(EnquiryFilter filter, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.EnquiryView, "view enquiries");
                return await _enquiryDataAccess.GetEnquiriesAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiries");
                throw;
            }
        }

        public async Task<EnquiryModel?> GetEnquiryAsync(string id, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.EnquiryView, "view enquiries");
                return await _enquiryDataAccess.GetEnquiryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiry by ID: {id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel updateModel, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.EnquiryEdit, "update enquiries");

                var validationResult = await _createEnquiryValidator.ValidateAsync(updateModel, options => options.IncludeRuleSets("default", "Update"));
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existingEnquiry = await _enquiryDataAccess.GetEnquiryAsync(updateModel.Id);
                if (existingEnquiry == null)
                {
                    throw new ArgumentException("Enquiry not found");
                }

                var hasExistingResponse = !string.IsNullOrWhiteSpace(existingEnquiry.Response);
                if (hasExistingResponse && updateModel.Response != existingEnquiry.Response)
                {
                    throw new ArgumentException("This enquiry has already been responded to and its response message cannot be changed.");
                }

                var previousStatus = existingEnquiry.Status;
                existingEnquiry.ApplyEditableFields(updateModel);

                var updated = await _enquiryDataAccess.UpdateEnquiriesAsync(existingEnquiry.Id, existingEnquiry);

                if (updated)
                {
                    var statusChanged = existingEnquiry.Status != previousStatus;
                    await _notificationPublisher.PublishToRoleAsync(
                        module: "Enquiry",
                        entityId: existingEnquiry.Id,
                        summary: statusChanged
                            ? $"Enquiry status changed to {existingEnquiry.Status}"
                            : $"Enquiry updated for {existingEnquiry.FirstName} {existingEnquiry.LastName}",
                        role: statusChanged ? SystemRole.Manager : SystemRole.Staff);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Enquiry: {id}", updateModel.Id);
                throw;
            }
        }

        public async Task<bool> DeleteEnquiriesAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnquiryDelete, "delete enquiries");

            var deleted = await _enquiryDataAccess.DeleteEnquiriesAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted enquiry with ID: {EnquiryId}", id);
            }
            return deleted;
        }

        public async Task<Dictionary<string, int>> GetMonthlyCountsAsync(string userId, DateTime since)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnquiryView, "view enquiries");
            return await _enquiryDataAccess.GetMonthlyCountsAsync(since);
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync(string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnquiryView, "view enquiries");
            return await _enquiryDataAccess.GetStatusCountsAsync();
        }
    }
}