using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.BusinessPartner;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class BusinessPartnerService : IBusinessPartnerService
    {
        private readonly IBusinessPartner _businessPartnerDataAccess;
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly IValidator<BusinessPartnerModel> _businessPartnerValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<BusinessPartnerService> _logger;

        public BusinessPartnerService(
            IBusinessPartner businessPartnerDataAccess,
            IEducationPartner educationPartnerDataAccess,
            IValidator<BusinessPartnerModel> businessPartnerValidator,
            IPermissionService permissionService,
            ILogger<BusinessPartnerService> logger)
        {
            _businessPartnerDataAccess = businessPartnerDataAccess;
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _businessPartnerValidator = businessPartnerValidator;
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

        // Auth: requires BusinessPartnersView permission (staff detail page).
        public async Task<BusinessPartnerModel?> GetBusinessPartnerById(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.BusinessPartnersView, "view business partners");
            return await _businessPartnerDataAccess.GetBusinessPartnerByIdAsync(id);
        }

        // Auth: requires BusinessPartnersAdd permission (staff-only).
        public async Task<bool> CreateBusinessPartner(BusinessPartnerModel partner, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.BusinessPartnersAdd, "create business partners");

            var validate = await _businessPartnerValidator.ValidateAsync(partner);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for business partner creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            partner.Id = string.Empty;
            var created = await _businessPartnerDataAccess.CreateBusinessPartnerAsync(partner);
            _logger.LogInformation("Created new business partner with ID: {BusinessPartnerId} for {Name}", partner.Id, partner.Name);
            return created;
        }

        // Auth: none — deliberately open. See interface doc comment.
        public async Task<PagedResult<BusinessPartnerModel>> GetBusinessPartners(BusinessPartnerFilter filter)
        {
            return await _businessPartnerDataAccess.GetBusinessPartnersAsync(filter);
        }

        // Auth: none — deliberately open. See interface doc comment.
        public async Task<List<BusinessPartnerModel>> GetBusinessPartnersByIds(IEnumerable<string> ids)
        {
            return await _businessPartnerDataAccess.GetByIdsAsync(ids);
        }

        // Auth: requires BusinessPartnersView permission (staff-only).
        public async Task<PagedResult<BusinessPartnerModel>> SearchBusinessPartners(BusinessPartnerFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.BusinessPartnersView, "view business partners");
            return await GetBusinessPartners(filter);
        }

        // Auth: requires BusinessPartnersEdit permission (staff-only).
        public async Task<bool> UpdateBusinessPartner(string id, BusinessPartnerModel partner, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.BusinessPartnersEdit, "update business partners");

            var validate = await _businessPartnerValidator.ValidateAsync(partner);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for business partner update: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _businessPartnerDataAccess.GetBusinessPartnerByIdAsync(id);
            if (existing == null)
            {
                throw new ArgumentException("Business partner not found");
            }

            existing.ApplyEditableFields(partner);

            var updated = await _businessPartnerDataAccess.UpdateBusinessPartnerAsync(id, existing);
            if (updated)
            {
                _logger.LogInformation("Updated business partner with ID: {BusinessPartnerId}", id);
            }
            return updated;
        }

        // Auth: requires BusinessPartnersDelete permission (staff-only). Blocked if any
        // Education Partner is still "Managed under" this Business Partner — that link
        // must be cleared/reassigned first, matching the pattern of Education Partner's
        // own cascade-on-delete for its Courses (here we refuse instead of cascading,
        // since removing the link silently would be a surprising side effect on a
        // different module's record).
        public async Task<bool> DeleteBusinessPartner(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.BusinessPartnersDelete, "delete business partners");

            if (await _educationPartnerDataAccess.ExistsWithBusinessPartnerIdAsync(id))
            {
                throw new ArgumentException("Cannot delete: one or more education partners are still managed under this business partner.");
            }

            var deleted = await _businessPartnerDataAccess.DeleteBusinessPartnerAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted business partner with ID: {BusinessPartnerId}", id);
            }
            return deleted;
        }
    }
}
