using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Financial
{
    // Renamed from InvoiceModel to avoid a duplicate class name with the active
    // ShareService.Models.Invoice.InvoiceModel ledger this one was replaced by (see that
    // class's remarks) — this type is legacy/embedded and nothing writes to it anymore.
    public class LegacyInvoiceModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("invoiceNo")]
        public string InvoiceNo { get; set; } = string.Empty;

        // "BusinessPartner" | "EducationPartner" — whichever party owes Eduflex commission
        // for this period; direct-contract enrolments (no linked Business Partner) invoice
        // the Education Partner instead.
        [BsonElement("invoiceToType")]
        public string InvoiceToType { get; set; } = string.Empty;

        [BsonElement("invoiceToId")]
        public string InvoiceToId { get; set; } = string.Empty;

        // Snapshot of the payer's name at invoice time, so the invoice stays legible even
        // if that partner is later renamed.
        [BsonElement("invoiceToName")]
        public string InvoiceToName { get; set; } = string.Empty;

        // Snapshot of the enrolled student's name — distinct from InvoiceToName (the
        // paying partner) — used in the PDF filename convention.
        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("periodStart")]
        public DateTime PeriodStart { get; set; }

        [BsonElement("periodEnd")]
        public DateTime PeriodEnd { get; set; }

        [BsonElement("periodTotal")]
        public decimal PeriodTotal { get; set; }

        // The rich-text editor's HTML content at save time — this is what gets rendered
        // to PDF, so it IS the invoice, not just a description of it.
        [BsonElement("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;

        // Draft | Generated
        [BsonElement("status")]
        public string Status { get; set; } = "Draft";

        [BsonElement("pdfUrl")]
        public string? PdfUrl { get; set; }

        [BsonElement("pdfFileName")]
        public string? PdfFileName { get; set; }

        [BsonElement("generatedAt")]
        public DateTime? GeneratedAt { get; set; }

        [BsonElement("createdByUserId")]
        public string? CreatedByUserId { get; set; }

        [BsonElement("createdByName")]
        public string CreatedByName { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
