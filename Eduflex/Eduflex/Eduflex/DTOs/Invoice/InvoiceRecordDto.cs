namespace Eduflex.DTOs.Invoice
{
    public class InvoiceClaimItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstRatePercent { get; set; } = 10m;
    }


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
        public string? RelatedFinancialRecordId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Total { get; set; }
        public List<InvoiceClaimItemDto> ClaimItems { get; set; } = new();
        public string? StudentName { get; set; }
        public string? StudentRefCode { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentPhone { get; set; }
        public string? CourseName { get; set; }
        public string? Campus { get; set; }
        public string? EducationPartnerName { get; set; }
        public string? CustomContent { get; set; }
        public string? PdfUrl { get; set; }
        public string? PdfFileName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentEvidenceUrl { get; set; }
        public string? LastEmailError { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        // Returned so the Resend dialog can prefill the message that was last sent.
        public string EmailSubject { get; set; } = string.Empty;
        public string EmailBody { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class CancelInvoiceDto
    {
        public string? Reason { get; set; }
    }

    public class ResendInvoiceDto
    {
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
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
        public string? RelatedFinancialRecordId { get; set; }
        public string? RelatedInvoicePlanEntryId { get; set; }
        public string? RelatedStudentPlanEntryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstRatePercent { get; set; } = 10m;
        public List<InvoiceClaimItemDto> ClaimItems { get; set; } = new();
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

    public class ConfirmInvoicePaymentDto
    {
        public string? PaymentEvidenceUrl { get; set; }
    }
}
