using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.EducationPartner;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class EducationPartnerService : IEducationPartnerService
    {
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly ICourse _courseDataAccess;
        private readonly IValidator<EducationPartnerModel> _educationPartnerValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<EducationPartnerService> _logger;

        public EducationPartnerService(
            IEducationPartner educationPartnerDataAccess,
            ICourse courseDataAccess,
            IValidator<EducationPartnerModel> educationPartnerValidator,
            IPermissionService permissionService,
            ILogger<EducationPartnerService> logger)
        {
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _courseDataAccess = courseDataAccess;
            _educationPartnerValidator = educationPartnerValidator;
            _permissionService = permissionService;
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

        // Auth: requires EducationPartnersView permission (staff detail page).
        public async Task<EducationPartnerModel?> GetEducationPartnerById(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersView, "view education partners");
            return await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(id);
        }

        // Auth: requires EducationPartnersAdd permission (staff-only).
        public async Task<bool> CreateEducationPartner(EducationPartnerModel partner, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersAdd, "create education partners");

            var validate = await _educationPartnerValidator.ValidateAsync(partner);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for education partner creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            partner.Id = string.Empty;
            var created = await _educationPartnerDataAccess.CreateEducationPartnerAsync(partner);
            _logger.LogInformation("Created new education partner with ID: {EducationPartnerId} for {Name}", partner.Id, partner.Name);
            return created;
        }

        // Auth: none — deliberately open. Shared by the student-facing directory
        // (any authenticated user, no permission required) and SearchEducationPartners
        // below (staff-gated). Do not add a check here; add it to a new wrapper instead,
        // the way SearchEducationPartners already does.
        public async Task<PagedResult<EducationPartnerModel>> GetEducationPartners(EducationPartnerFilter filter)
        {
            return await _educationPartnerDataAccess.GetEducationPartnersAsync(filter);
        }

        // Auth: requires EducationPartnersView permission (staff-only) — the gated
        // counterpart to the open GetEducationPartners above.
        public async Task<PagedResult<EducationPartnerModel>> SearchEducationPartners(EducationPartnerFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersView, "view education partners");
            return await GetEducationPartners(filter);
        }

        // Auth: requires EducationPartnersEdit permission (staff-only).
        public async Task<bool> UpdateEducationPartner(string id, EducationPartnerModel partner, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersEdit, "update education partners");

            var validate = await _educationPartnerValidator.ValidateAsync(partner);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for education partner update: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(id);
            if (existing == null)
            {
                throw new ArgumentException("Education partner not found");
            }

            existing.ApplyEditableFields(partner);

            var updated = await _educationPartnerDataAccess.UpdateEducationPartnerAsync(id, existing);
            if (updated)
            {
                _logger.LogInformation("Updated education partner with ID: {EducationPartnerId}", id);
            }
            return updated;
        }

        // Auth: requires EducationPartnersDelete permission (staff-only).
        public async Task<bool> DeleteEducationPartner(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersDelete, "delete education partners");

            var deleted = await _educationPartnerDataAccess.DeleteEducationPartnerAsync(id);
            if (deleted)
            {
                await _courseDataAccess.DeleteByPartnerIdAsync(id);
                _logger.LogInformation("Deleted education partner with ID: {EducationPartnerId} (cascaded course deletion)", id);
            }
            return deleted;
        }
    }
}
