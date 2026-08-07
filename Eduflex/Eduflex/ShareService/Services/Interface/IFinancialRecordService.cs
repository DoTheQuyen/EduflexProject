using ShareService.Common;
using ShareService.Models.Enrolment;
using ShareService.Models.Financial;

namespace ShareService.Services.Interface
{
    public interface IFinancialRecordService
    {
        // Auth: none — internal, system-triggered hook called from
        // EnrolmentService.CompleteVisaStepAsync once VISA Outcome is Granted. The caller
        // has already passed an EnrolmentsEdit check; this just creates the record.
        // Idempotent — returns the existing record if one already exists for this enrolment.
        Task<FinancialRecordModel> CreateForEnrolmentIfNotExistsAsync(EnrolmentModel enrolment, string actingUserId);

        Task<FinancialRecordModel?> GetByIdAsync(string id, string userId);
        Task<FinancialRecordModel?> GetByEnrolmentIdAsync(string enrolmentId, string userId);
        Task<PagedResult<FinancialRecordModel>> SearchAsync(FinancialRecordFilter filter, string userId);

        Task<CommissionAdjustmentModel> AddCommissionAdjustmentAsync(string id, string reason, decimal amount, string actingUserId);

        Task<InvoiceModel> CreateInvoiceDraftAsync(string id, string invoiceNo, string invoiceToType, string invoiceToId, string invoiceToName, string studentName,
            DateTime periodStart, DateTime periodEnd, decimal periodTotal, string htmlContent, string actingUserId);

        Task<bool> UpdateInvoiceDraftAsync(string id, string invoiceId, string htmlContent, decimal periodTotal, string actingUserId);
        Task<InvoiceModel> GenerateInvoicePdfAsync(string id, string invoiceId, string actingUserId);
        Task<Uri> GetInvoiceDownloadLinkAsync(string id, string invoiceId, string userId);

        Task<FinancialCommunicationModel> SendCommunicationAsync(string id, string toEmail, string recipientType, string subject, string body,
            string? templateKey, string? relatedInvoiceId, string actingUserId);

        Task<FinancialRecordModel> RegenerateInvoicePlanAsync(string id, string actingUserId);
        Task<FinancialRecordModel> UpdatePlanEntryDateAsync(string id, string entryId, DateTime claimDate, string actingUserId);
        Task<FinancialRecordModel> SkipPlanEntryAsync(string id, string entryId, string? reason, string actingUserId);
        Task<FinancialRecordModel> RestorePlanEntryAsync(string id, string entryId, string actingUserId);
        Task<FinancialRecordModel> AddManualPlanEntryAsync(string id, DateTime claimDate, string actingUserId);
    }
}
