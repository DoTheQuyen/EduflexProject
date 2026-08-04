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
    }

    public static class InvoiceStatuses
    {
        public const string Sent = "Sent";
        public const string Paid = "Paid";
    }

    // Top-level ledger of every invoice ever sent through the new Invoice Template
    // system (student service fees today; partner commission invoices remain on the
    // existing, untouched FinancialRecord.Invoices embedded list for now). One
    // collection, one global numbering source of truth per template.
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

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("gstAmount")]
        public decimal GstAmount { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }

        [BsonElement("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;

        [BsonElement("pdfUrl")]
        public string? PdfUrl { get; set; }

        [BsonElement("pdfFileName")]
        public string? PdfFileName { get; set; }

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
