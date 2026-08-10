namespace ShareService.Models.Invoice
{
    // Input to InvoiceService.SendInvoiceAsync — not a Mongo document, just a plain
    // carrier between the controller/DTO layer and the service, mirroring how
    // FinancialRecordService's Create/Update invoice methods take loose parameters
    // rather than a persisted-shape model.
    public class SendInvoiceRequestModel
    {
        public string TemplateId { get; set; } = string.Empty;
        public string RecipientType { get; set; } = InvoiceRecipientTypes.Student;
        public string? RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string? RelatedEnrolmentId { get; set; }
        public string? RelatedStepKey { get; set; }
        public string? RelatedFinancialRecordId { get; set; }
        // When set, marks this specific FinancialRecord.InvoicePlan entry as Invoiced once
        // the send succeeds (see InvoiceService.RecordFinancialActivityAsync) — the Finance
        // tab's claim schedule can then show "this claim is done" without a separate call.
        public string? RelatedInvoicePlanEntryId { get; set; }
        // Same idea as RelatedInvoicePlanEntryId above, but for the student instalment
        // schedule (StudentPaymentPlanEntryModel) instead of the partner claim schedule
        // — set when this send fulfils one instalment of a student's payment plan.
        public string? RelatedStudentPlanEntryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstRatePercent { get; set; } = 10m;
        public List<InvoiceClaimItem> ClaimItems { get; set; } = new();
        public string? StudentName { get; set; }
        public string? StudentRefCode { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentPhone { get; set; }
        public string? CourseName { get; set; }
        public string? Campus { get; set; }
        public string? EducationPartnerName { get; set; }
        public string? CustomContent { get; set; }
        public string EmailSubject { get; set; } = string.Empty;
        public string EmailBody { get; set; } = string.Empty;
    }
}
