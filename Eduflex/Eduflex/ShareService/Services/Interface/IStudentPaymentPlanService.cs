using ShareService.Models.StudentPaymentPlan;

namespace ShareService.Services.Interface
{
    public interface IStudentPaymentPlanService
    {
        Task<List<StudentPaymentPlanEntryModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId);

        // Even-split generator: TotalAmount divided across InstalmentCount entries,
        // spaced IntervalMonths apart starting FirstDueDate. Only allowed when the
        // enrolment has no plan yet — unlike the partner claim schedule (regenerated
        // whenever the course's intake calendar changes), a student's instalment total
        // is fixed once agreed, so there's no recurring need to reconcile a regenerated
        // plan against invoiced/skipped entries.
        Task<List<StudentPaymentPlanEntryModel>> GeneratePlanAsync(
            string enrolmentId, string studentName, string? courseName, string feeType,
            decimal totalAmount, int instalmentCount, DateTime firstDueDate, int intervalMonths, string actingUserId);

        Task<StudentPaymentPlanEntryModel> AddManualEntryAsync(
            string enrolmentId, string studentName, string? courseName, string feeType, string label,
            decimal amount, DateTime dueDate, string actingUserId);

        Task<StudentPaymentPlanEntryModel> UpdateEntryDateAsync(string entryId, DateTime dueDate, string actingUserId);
        Task<StudentPaymentPlanEntryModel> SkipEntryAsync(string entryId, string? reason, string actingUserId);
        Task<StudentPaymentPlanEntryModel> RestoreEntryAsync(string entryId, string actingUserId);

        // Marks an entry Invoiced and links it — called from InvoiceService.SendInvoiceAsync
        // when a send carries RelatedStudentPlanEntryId, mirroring how FinancialRecordService
        // marks an InvoicePlanEntry Invoiced today. Internal hook, no permission check of its
        // own — the caller has already authorized the send itself.
        Task MarkEntryInvoicedAsync(string entryId, string invoiceId);
    }
}
