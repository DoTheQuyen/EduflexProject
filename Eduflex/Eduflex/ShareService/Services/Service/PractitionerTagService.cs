using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.VisaProcess;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class PractitionerTagService : IPractitionerTagService
    {
        private readonly IPractitionerTag _tagDataAccess;
        private readonly IValidator<PractitionerTagModel> _validator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PractitionerTagService> _logger;

        public PractitionerTagService(
            IPractitionerTag tagDataAccess,
            IValidator<PractitionerTagModel> validator,
            IPermissionService permissionService,
            ILogger<PractitionerTagService> logger)
        {
            _tagDataAccess = tagDataAccess;
            _validator = validator;
            _permissionService = permissionService;
            _logger = logger;
        }

        private async Task RequireManagePermissionAsync(string userId, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.VisaProcessTemplatesEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // No permission check — see IPractitionerTagService.GetAllAsync.
        public async Task<List<PractitionerTagModel>> GetAllAsync()
        {
            return await _tagDataAccess.GetAllAsync();
        }

        public async Task<PractitionerTagModel> CreateAsync(PractitionerTagModel tag, string userId)
        {
            await RequireManagePermissionAsync(userId, "create practitioner tags");

            var validation = await _validator.ValidateAsync(tag);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            tag.Id = string.Empty;
            await _tagDataAccess.CreateAsync(tag);
            _logger.LogInformation("Created practitioner tag {TagId} ({Name})", tag.Id, tag.Name);
            return tag;
        }

        public async Task<bool> UpdateAsync(string id, PractitionerTagModel tag, string userId)
        {
            await RequireManagePermissionAsync(userId, "update practitioner tags");

            var existing = await _tagDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Practitioner tag not found");

            var validation = await _validator.ValidateAsync(tag);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            existing.ApplyEditableFields(tag);
            return await _tagDataAccess.ReplaceAsync(id, existing);
        }

        // Deactivate/reactivate only — see PractitionerTagModel.Active.
        public async Task<bool> SetActiveAsync(string id, bool isActive, string userId)
        {
            await RequireManagePermissionAsync(userId, isActive ? "activate practitioner tags" : "deactivate practitioner tags");

            var existing = await _tagDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Practitioner tag not found");

            existing.Active = isActive;
            return await _tagDataAccess.ReplaceAsync(id, existing);
        }
    }
}
