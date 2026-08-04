namespace Eduflex.DTOs.Invoice
{
    // Named InvoiceRecordDto (not InvoiceDto) because Swashbuckle's default schemaId is
    // the bare class name — Eduflex.DTOs.Financial.InvoiceDto already claims "InvoiceDto".
    public class InvoiceRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public string? RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string? RelatedEnrolmentId { get; set; }
        public string? RelatedStepKey { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Total { get; set; }
        public string? PdfUrl { get; set; }
        public string? PdfFileName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentEvidenceUrl { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class SendInvoiceDto
    {
        public string TemplateId { get; set; } = string.Empty;
        public string RecipientType { get; set; } = "Student";
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

    public class ConfirmInvoicePaymentDto
    {
        public string? PaymentEvidenceUrl { get; set; }
    }
}
