using ShareService.Models.Invoice;

namespace ShareService.DataAccess.Interface
{
    public interface IInvoiceTemplate
    {
        Task<List<InvoiceTemplateModel>> GetAllAsync();
        Task<InvoiceTemplateModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(InvoiceTemplateModel template);
        Task<bool> ReplaceAsync(string id, InvoiceTemplateModel template);

        // Atomically reserves the next sequence number for this template — returns the
        // number to use (Before the increment), leaving NextSequence already advanced.
        // Returns null if the template doesn't exist.
        Task<int?> ReserveNextSequenceAsync(string templateId);
    }
}
