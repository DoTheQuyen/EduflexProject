using ShareService.Models.Invoice;

namespace ShareService.DataAccess.Interface
{
    public interface IInvoice
    {
        Task<List<InvoiceModel>> GetAllAsync(string? category, string? status);
        Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId);
        Task<InvoiceModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(InvoiceModel invoice);
        Task<bool> ReplaceAsync(string id, InvoiceModel invoice);
    }
}
