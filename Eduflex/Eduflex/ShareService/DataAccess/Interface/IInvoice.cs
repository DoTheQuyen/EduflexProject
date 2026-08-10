using ShareService.Common;
using ShareService.Models.Invoice;

namespace ShareService.DataAccess.Interface
{
    public interface IInvoice
    {
        Task<PagedResult<InvoiceModel>> GetAllAsync(string? category, string? status, int pageNumber, int pageSize);
        Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId);
        Task<List<InvoiceModel>> GetByFinancialRecordIdAsync(string financialRecordId);
        Task<InvoiceModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(InvoiceModel invoice);
        Task<bool> ReplaceAsync(string id, InvoiceModel invoice);
    }
}
