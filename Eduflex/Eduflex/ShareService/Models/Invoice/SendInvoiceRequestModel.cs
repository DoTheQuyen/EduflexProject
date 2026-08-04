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
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstRatePercent { get; set; } = 10m;
        public string EmailSubject { get; set; } = string.Empty;
        public string EmailBody { get; set; } = string.Empty;
    }
}
