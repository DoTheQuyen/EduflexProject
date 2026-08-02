using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.Enrolment;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IEmailTemplate _emailTemplateDataAccess;
        private readonly IValidator<EmailTemplateModel> _validator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            IEmailTemplate emailTemplateDataAccess,
            IValidator<EmailTemplateModel> validator,
            IPermissionService permissionService,
            ILogger<EmailTemplateService> logger)
        {
            _emailTemplateDataAccess = emailTemplateDataAccess;
            _validator = validator;
            _permissionService = permissionService;
            _logger = logger;
        }

        private async Task RequireManagePermissionAsync(string userId, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.EmailTemplatesEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // No permission check — any authenticated staff member composing an enrolment/finance
        // email needs this list to populate their template picker, same reasoning as
        // IDynamicFormTemplateService.GetAllAsync. The gated GetById/Create/Update/SetStatus
        // below are the admin management screen.
        public async Task<List<EmailTemplateModel>> GetAllAsync()
        {
            return await _emailTemplateDataAccess.GetAllAsync();
        }

        public async Task<EmailTemplateModel?> GetByIdAsync(string id, string userId)
        {
            await RequireManagePermissionAsync(userId, "view this template's details");
            return await _emailTemplateDataAccess.GetByIdAsync(id);
        }

        public async Task<EmailTemplateModel> CreateAsync(EmailTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "create email templates");

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _emailTemplateDataAccess.GetByKeyAsync(template.Key);
            if (existing != null)
            {
                throw new ArgumentException("A template with this key already exists");
            }

            template.Id = string.Empty;
            template.IsSystemDefault = false;
            template.IsActive = true;
            await _emailTemplateDataAccess.CreateAsync(template);
            return template;
        }

        public async Task<bool> UpdateAsync(string id, EmailTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "update email templates");

            var existing = await _emailTemplateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Template not found");

            // Key is immutable (the update DTO never carries one — templates are addressed by
            // id on this route) but the validator still requires it, so borrow the real one from
            // the existing record before validating rather than leaving it empty.
            template.Key = existing.Key;

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            existing.ApplyEditableFields(template);
            return await _emailTemplateDataAccess.ReplaceAsync(id, existing);
        }

        // Deactivate/reactivate only — templates are never hard-deleted, matching Dynamic
        // Forms' template management (see DynamicFormTemplateService.SetStatusAsync).
        public async Task<bool> SetStatusAsync(string id, bool isActive, string userId)
        {
            await RequireManagePermissionAsync(userId, isActive ? "activate email templates" : "deactivate email templates");

            var existing = await _emailTemplateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Template not found");

            existing.IsActive = isActive;
            return await _emailTemplateDataAccess.ReplaceAsync(id, existing);
        }
    }
}
