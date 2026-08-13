using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.VisaProcess;
using ShareService.Mapping;
using ShareService.Models.VisaProcess;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class VisaProcessTemplateService : IVisaProcessTemplateService
    {
        private readonly IVisaProcessTemplate _templateDataAccess;
        private readonly IValidator<VisaProcessTemplateModel> _validator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<VisaProcessTemplateService> _logger;

        public VisaProcessTemplateService(
            IVisaProcessTemplate templateDataAccess,
            IValidator<VisaProcessTemplateModel> validator,
            IPermissionService permissionService,
            ILogger<VisaProcessTemplateService> logger)
        {
            _templateDataAccess = templateDataAccess;
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

        // No permission check — see IVisaProcessTemplateService.GetAllAsync.
        public async Task<List<VisaProcessTemplateModel>> GetAllAsync()
        {
            return await _templateDataAccess.GetAllAsync();
        }

        public async Task<VisaProcessTemplateModel?> GetByIdAsync(string id, string userId)
        {
            await RequireManagePermissionAsync(userId, "view this template's details");
            return await _templateDataAccess.GetByIdAsync(id);
        }

        public async Task<VisaProcessTemplateModel> CreateAsync(VisaProcessTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "create VISA process templates");

            NormalizeStepOrder(template);

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            template.Id = string.Empty;
            template.Version = 1;

            if (template.IsDefaultForCountry)
            {
                await ClearOtherDefaultsAsync(template.Country, template.Category, excludeId: null);
            }

            await _templateDataAccess.CreateAsync(template);
            _logger.LogInformation("Created VISA process template {TemplateId} ({Name})", template.Id, template.Name);
            return template;
        }

        public async Task<bool> UpdateAsync(string id, VisaProcessTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "update VISA process templates");

            var existing = await _templateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("VISA process template not found");

            NormalizeStepOrder(template);

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            if (template.IsDefaultForCountry)
            {
                await ClearOtherDefaultsAsync(template.Country, template.Category, excludeId: id);
            }

            existing.ApplyEditableFields(template);
            existing.Version += 1;
            return await _templateDataAccess.ReplaceAsync(id, existing);
        }

        // Deactivate/reactivate only — templates are never hard-deleted, since this module
        // isn't wired into any enrolment yet, but the same non-destructive pattern as every
        // other template catalog in this codebase (DynamicFormTemplateModel, EmailTemplateModel)
        // is worth keeping from the start.
        public async Task<bool> SetStatusAsync(string id, bool isActive, string userId)
        {
            await RequireManagePermissionAsync(userId, isActive ? "activate VISA process templates" : "deactivate VISA process templates");

            var existing = await _templateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("VISA process template not found");

            existing.Status = isActive ? VisaTemplateStatus.Active.ToString() : VisaTemplateStatus.Inactive.ToString();
            return await _templateDataAccess.ReplaceAsync(id, existing);
        }

        // Enforces "one default template per Country+Category" (docs/09 §F.6) — the
        // collection is small enough that a full scan here is simpler and more honest than
        // a specialized query, matching this project's avoid-overengineering convention.
        private async Task ClearOtherDefaultsAsync(string country, string category, string? excludeId)
        {
            var all = await _templateDataAccess.GetAllAsync();
            var others = all.Where(t => t.Country == country && t.Category == category
                && t.IsDefaultForCountry && t.Id != excludeId);

            foreach (var other in others)
            {
                other.IsDefaultForCountry = false;
                await _templateDataAccess.ReplaceAsync(other.Id, other);
            }
        }

        private static void NormalizeStepOrder(VisaProcessTemplateModel template)
        {
            for (var i = 0; i < template.Steps.Count; i++)
            {
                template.Steps[i].Order = i;
            }
        }
    }
}
