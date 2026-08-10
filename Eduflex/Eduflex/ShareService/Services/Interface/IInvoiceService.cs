using ShareService.Common;
using ShareService.Models.Invoice;

namespace ShareService.Services.Interface
{
    public interface IInvoiceService
    {
        Task<PagedResult<InvoiceModel>> GetAllAsync(string? category, string? status, int pageNumber, int pageSize, string userId);
        Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId);
        Task<List<InvoiceModel>> GetByFinancialRecordIdAsync(string financialRecordId, string userId);
        Task<InvoiceModel> SendInvoiceAsync(SendInvoiceRequestModel request, string actingUserId, string? actingUserRole);
        Task<Uri> GetDownloadLinkAsync(string invoiceId, string userId);
        Task<InvoiceModel> ConfirmPaymentAsync(string invoiceId, string? paymentEvidenceUrl, string actingUserId);
        Task<InvoiceModel> ResendInvoiceAsync(string invoiceId, string? emailSubject, string? emailBody, string actingUserId);
        Task<InvoiceModel> CancelInvoiceAsync(string invoiceId, string? reason, string actingUserId);
    }
}
