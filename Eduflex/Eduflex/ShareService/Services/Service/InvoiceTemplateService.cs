using FluentValidation;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.Invoice;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class InvoiceTemplateService : IInvoiceTemplateService
    {
        private readonly IInvoiceTemplate _invoiceTemplateDataAccess;
        private readonly IValidator<InvoiceTemplateModel> _validator;
        private readonly IPermissionService _permissionService;

        public InvoiceTemplateService(
            IInvoiceTemplate invoiceTemplateDataAccess,
            IValidator<InvoiceTemplateModel> validator,
            IPermissionService permissionService)
        {
            _invoiceTemplateDataAccess = invoiceTemplateDataAccess;
            _validator = validator;
            _permissionService = permissionService;
        }

        private async Task RequireManagePermissionAsync(string userId, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.InvoiceTemplatesEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        public async Task<List<InvoiceTemplateModel>> GetAllAsync()
        {
            return await _invoiceTemplateDataAccess.GetAllAsync();
        }

        public async Task<InvoiceTemplateModel?> GetByIdAsync(string id, string userId)
        {
            await RequireManagePermissionAsync(userId, "view this invoice template's details");
            return await _invoiceTemplateDataAccess.GetByIdAsync(id);
        }

        public async Task<InvoiceTemplateModel> CreateAsync(InvoiceTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "create invoice templates");

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            template.Id = string.Empty;
            template.IsActive = true;
            template.NextSequence = 1;
            await _invoiceTemplateDataAccess.CreateAsync(template);
            return template;
        }

        public async Task<bool> UpdateAsync(string id, InvoiceTemplateModel template, string userId)
        {
            await RequireManagePermissionAsync(userId, "update invoice templates");

            var existing = await _invoiceTemplateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Invoice template not found");

            // Category/prefix/padding/nextSequence are immutable after creation — see
            // ApplyEditableFields for why (changing the numbering scheme mid-sequence
            // would make already-issued invoice numbers ambiguous).
            template.Category = existing.Category;
            template.InvoiceNoPrefix = existing.InvoiceNoPrefix;
            template.NumberPadding = existing.NumberPadding;
            template.NextSequence = existing.NextSequence;

            var validation = await _validator.ValidateAsync(template);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            existing.ApplyEditableFields(template);
            return await _invoiceTemplateDataAccess.ReplaceAsync(id, existing);
        }

        public async Task<bool> SetStatusAsync(string id, bool isActive, string userId)
        {
            await RequireManagePermissionAsync(userId, isActive ? "activate invoice templates" : "deactivate invoice templates");

            var existing = await _invoiceTemplateDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Invoice template not found");

            existing.IsActive = isActive;
            return await _invoiceTemplateDataAccess.ReplaceAsync(id, existing);
        }
    }
}
