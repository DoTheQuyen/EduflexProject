using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Invoice
{
    public static class InvoiceRecipientTypes
    {
        public const string Student = "Student";
        public const string EducationPartner = "EducationPartner";
        public const string BusinessPartner = "BusinessPartner";
        // Custom-category invoices: a typed-in name/email with no backing record.
        public const string Custom = "Custom";
    }

    // Sent/Paid are the happy path. Failed means the invoice + PDF exist but the
    // notification email didn't go out (see InvoiceService.SendInvoiceAsync) — it stays
    // in the ledger so the reserved invoice number is never silently lost, and can be
    // retried via Resend. Cancelled is a soft void: the record stays for audit but is
    // excluded from income/already-invoiced totals.
    public static class InvoiceStatuses
    {
        public const string Sent = "Sent";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";

        // The statuses that represent real, billable income — used anywhere an
        // "already invoiced" or revenue total is computed.
        public static bool CountsAsIncome(string status) => status == Sent || status == Paid;
    }

    // One line of a Partner invoice's multi-item claim (e.g. separate service-fee and
    // late-admin-fee lines on one commission invoice). Only Partner invoices use this —
    // Customer/Custom invoices stay on the single Description/Amount/GstRatePercent
    // fields below, so an empty list here means "single-line invoice", not "no items".
    public class InvoiceClaimItem
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GstRatePercent { get; set; } = 10m;
    }

    // Top-level ledger of every invoice ever sent through the Invoice Template system —
    // student service fees, partner commission claims, and custom one-offs all write
    // here now. FinancialRecordModel.Invoices (ShareService.Models.Financial) is the
    // older, embedded ledger this one replaced; its write endpoints are no longer
    // reachable from the UI (see FinancialRecordsController). One collection, one
    // global numbering source of truth per template.
    public class InvoiceModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("invoiceNo")]
        public string InvoiceNo { get; set; } = string.Empty;

        [BsonElement("templateId")]
        public string TemplateId { get; set; } = string.Empty;

        [BsonElement("category")]
        public string Category { get; set; } = InvoiceTemplateCategories.Customer;

        [BsonElement("recipientType")]
        public string RecipientType { get; set; } = InvoiceRecipientTypes.Student;

        [BsonElement("recipientId")]
        public string? RecipientId { get; set; }

        [BsonElement("recipientName")]
        public string RecipientName { get; set; } = string.Empty;

        [BsonElement("recipientEmail")]
        public string RecipientEmail { get; set; } = string.Empty;

        // Links this invoice back to the Enrolment (and specific VISA Process step) it
        // was raised from, e.g. the Enrolment Form step's service-fee invoice. Optional —
        // a partner invoice raised outside an enrolment context wouldn't set these.
        [BsonElement("relatedEnrolmentId")]
        public string? RelatedEnrolmentId { get; set; }

        [BsonElement("relatedStepKey")]
        public string? RelatedStepKey { get; set; }

        // Links this invoice back to the FinancialRecord it was raised from — the Finance
        // module's partner commission invoices, mirroring RelatedEnrolmentId above.
        [BsonElement("relatedFinancialRecordId")]
        public string? RelatedFinancialRecordId { get; set; }

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("gstAmount")]
        public decimal GstAmount { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }

        // Partner invoices only — when non-empty, this is the source of truth for
        // Amount/GstAmount/Total above (which hold the summed totals) instead of the
        // single Description/Amount/GstRatePercent fields.
        [BsonElement("claimItems")]
        public List<InvoiceClaimItem> ClaimItems { get; set; } = new();

        // Snapshot of the linked Enrolment at send time, shown on Partner invoices only
        // (see InvoiceService.BuildInvoiceHtml) — a partner invoice can be raised for a
        // specific student's commission, so the recipient (the partner) can see which
        // enrolment it relates to. All optional since not every Partner invoice need be
        // tied to one enrolment.
        [BsonElement("studentName")]
        public string? StudentName { get; set; }

        [BsonElement("studentRefCode")]
        public string? StudentRefCode { get; set; }

        [BsonElement("studentEmail")]
        public string? StudentEmail { get; set; }

        [BsonElement("studentPhone")]
        public string? StudentPhone { get; set; }

        [BsonElement("courseName")]
        public string? CourseName { get; set; }

        [BsonElement("campus")]
        public string? Campus { get; set; }

        [BsonElement("educationPartnerName")]
        public string? EducationPartnerName { get; set; }

        // Custom-category invoices only — the free-text body staff typed, rendered in
        // place of the claim-items table inside the same branded shell.
        [BsonElement("customContent")]
        public string? CustomContent { get; set; }

        [BsonElement("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;

        [BsonElement("pdfUrl")]
        public string? PdfUrl { get; set; }

        [BsonElement("pdfFileName")]
        public string? PdfFileName { get; set; }

        // Stored so a failed send can be retried verbatim — without these a Resend would
        // have no message to re-send (the originals only ever existed on the request).
        [BsonElement("emailSubject")]
        public string EmailSubject { get; set; } = string.Empty;

        [BsonElement("emailBody")]
        public string EmailBody { get; set; } = string.Empty;

        // Populated when a send attempt fails, so staff can see why without digging
        // through server logs. Cleared on a successful resend.
        [BsonElement("lastEmailError")]
        public string? LastEmailError { get; set; }

        [BsonElement("cancelledAt")]
        public DateTime? CancelledAt { get; set; }

        [BsonElement("cancelReason")]
        public string? CancelReason { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = InvoiceStatuses.Sent;

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; }

        [BsonElement("paidAt")]
        public DateTime? PaidAt { get; set; }

        [BsonElement("paymentEvidenceUrl")]
        public string? PaymentEvidenceUrl { get; set; }

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonElement("createdByName")]
        public string CreatedByName { get; set; } = string.Empty;
    }
}
