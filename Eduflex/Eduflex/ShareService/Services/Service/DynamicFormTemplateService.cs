using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.DynamicForms;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.DynamicForm;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class DynamicFormTemplateService : IDynamicFormTemplateService
    {
        private readonly IDynamicFormTemplate _templateDataAccess;
        private readonly IValidator<DynamicFormTemplateModel> _validator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<DynamicFormTemplateService> _logger;

        public DynamicFormTemplateService(
            IDynamicFormTemplate templateDataAccess,
            IValidator<DynamicFormTemplateModel> validator,
            IPermissionService permissionService,
            ILogger<DynamicFormTemplateService> logger)
        {
            _templateDataAccess = templateDataAccess;
            _validator = validator;
            _permissionService = permissionService;
            _logger = logger;
        }

        private async Task RequireManagePermissionAsync(string userId, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.DynamicFormsEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // No permission check — see IDynamicFormTemplateService.GetAllAsync.
        public async Task<List<DynamicFormTemplateModel>> GetAllAsync()
        {
            return await _templateDataAccess.GetAllAsync();
        }

        public async Task<DynamicFormTemplateModel?> GetByIdAsync(string id, string userId)
        {
            await RequireManagePermissionAsync(userId, "view this form's details");
            return await _templateDataAccess.GetByIdAsync(id);
        }

        public async Task<DynamicFormTemplateModel> CreateAsync(DynamicFormTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "create dynamic forms");

            NormalizeQuestionOrder(template);

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            template.Id = string.Empty;
            await _templateDataAccess.CreateAsync(template);
            _logger.LogInformation("Created dynamic form template {TemplateId} ({Name})", template.Id, template.Name);
            return template;
        }

        public async Task<bool> UpdateAsync(string id, DynamicFormTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "update dynamic forms");

            var existing = await _templateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Form template not found");

            NormalizeQuestionOrder(template);

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            existing.ApplyEditableFields(template);
            return await _templateDataAccess.ReplaceAsync(id, existing);
        }

        // Deactivate/reactivate only — templates are never hard-deleted, since past
        // EnrolmentFormResponses reference them by id (FormTemplateId) even though the
        // response itself snapshots the questions independently.
        public async Task<bool> SetStatusAsync(string id, bool isActive, string userId)
        {
            await RequireManagePermissionAsync(userId, isActive ? "activate dynamic forms" : "deactivate dynamic forms");

            var existing = await _templateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Form template not found");

            existing.Status = isActive ? TemplateStatus.Active.ToString() : TemplateStatus.Inactive.ToString();
            return await _templateDataAccess.ReplaceAsync(id, existing);
        }

        private static void NormalizeQuestionOrder(DynamicFormTemplateModel template)
        {
            for (var i = 0; i < template.Questions.Count; i++)
            {
                template.Questions[i].Order = i;
                if (string.IsNullOrEmpty(template.Questions[i].Id))
                {
                    template.Questions[i].Id = Guid.NewGuid().ToString();
                }
            }
        }
    }
}
