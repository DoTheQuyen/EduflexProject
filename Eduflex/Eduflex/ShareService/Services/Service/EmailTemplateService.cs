using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
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

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // Auth: requires EnrolmentsView permission (staff-only — email templates reuse
        // Enrolments' permission keys rather than having their own module).
        public async Task<List<EmailTemplateModel>> GetAllAsync(string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnrolmentsView, "view email templates");
            return await _emailTemplateDataAccess.GetAllAsync();
        }

        // Auth: requires EnrolmentsEdit permission (staff-only).
        public async Task<EmailTemplateModel> CreateAsync(EmailTemplateModel template, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnrolmentsEdit, "create email templates");

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
            await _emailTemplateDataAccess.CreateAsync(template);
            return template;
        }

        // Auth: requires EnrolmentsEdit permission (staff-only).
        public async Task<bool> UpdateAsync(string id, EmailTemplateModel template, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnrolmentsEdit, "update email templates");

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

        // Auth: requires EnrolmentsEdit permission (staff-only).
        public async Task<bool> DeleteAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnrolmentsEdit, "delete email templates");

            var existing = await _emailTemplateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Template not found");

            if (existing.IsSystemDefault)
            {
                throw new ArgumentException("Built-in templates cannot be deleted.");
            }

            return await _emailTemplateDataAccess.DeleteAsync(id);
        }
    }
}
