using ShareService.Models.Invoice;

namespace ShareService.Services.Interface
{
    public interface IInvoiceTemplateService
    {
        // No permission check — any staff member sending an invoice needs this list to
        // populate their template picker, same reasoning as IEmailTemplateService.GetAllAsync.
        Task<List<InvoiceTemplateModel>> GetAllAsync();
        Task<InvoiceTemplateModel?> GetByIdAsync(string id, string userId);
        Task<InvoiceTemplateModel> CreateAsync(InvoiceTemplateModel template, string userId);
        Task<bool> UpdateAsync(string id, InvoiceTemplateModel template, string userId);
        Task<bool> SetStatusAsync(string id, bool isActive, string userId);
    }
}
