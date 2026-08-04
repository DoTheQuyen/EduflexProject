using ShareService.Models.Invoice;

namespace ShareService.Services.Interface
{
    public interface IInvoiceService
    {
        Task<List<InvoiceModel>> GetAllAsync(string? category, string? status, string userId);
        Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId);
        Task<InvoiceModel> SendInvoiceAsync(SendInvoiceRequestModel request, string actingUserId, string? actingUserRole);
        Task<Uri> GetDownloadLinkAsync(string invoiceId, string userId);
        Task<InvoiceModel> ConfirmPaymentAsync(string invoiceId, string? paymentEvidenceUrl, string actingUserId);
    }
}
