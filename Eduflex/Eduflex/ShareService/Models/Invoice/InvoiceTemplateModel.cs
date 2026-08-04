using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Invoice
{
    // Customer = student service-fee invoices, Partner = business/education partner
    // commission invoices. Kept as a plain string (not an enum) so a new category can be
    // added later without a schema migration, matching VisaProcessStepModel's convention.
    public static class InvoiceTemplateCategories
    {
        public const string Customer = "Customer";
        public const string Partner = "Partner";
    }

    public class InvoiceTemplateModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("category")]
        public string Category { get; set; } = InvoiceTemplateCategories.Customer;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("logoUrl")]
        public string? LogoUrl { get; set; }

        [BsonElement("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [BsonElement("senderAddressLines")]
        public List<string> SenderAddressLines { get; set; } = new();

        [BsonElement("senderAbn")]
        public string? SenderAbn { get; set; }

        [BsonElement("senderEmail")]
        public string SenderEmail { get; set; } = string.Empty;

        [BsonElement("senderPhone")]
        public string? SenderPhone { get; set; }

        [BsonElement("bankName")]
        public string? BankName { get; set; }

        [BsonElement("bankBsb")]
        public string? BankBsb { get; set; }

        [BsonElement("bankAccountNumber")]
        public string? BankAccountNumber { get; set; }

        [BsonElement("bankAccountName")]
        public string? BankAccountName { get; set; }

        // e.g. "INV-Eduflex" — combined with a zero-padded NextSequence to form the final
        // invoice number, e.g. "INV-Eduflex-0007".
        [BsonElement("invoiceNoPrefix")]
        public string InvoiceNoPrefix { get; set; } = "INV-Eduflex";

        [BsonElement("numberPadding")]
        public int NumberPadding { get; set; } = 4;

        // Default line-item values for this template. When set, the send-invoice panel
        // shows them read-only — staff pick a template and get the configured price, they
        // don't type one in. When left blank, the corresponding field opens up for free
        // input instead. DefaultAmount specifically allows a Manager/Admin override (see
        // InvoiceService.SendInvoiceAsync); DefaultDescription/DefaultGstRatePercent don't.
        [BsonElement("defaultDescription")]
        public string? DefaultDescription { get; set; }

        [BsonElement("defaultAmount")]
        public decimal? DefaultAmount { get; set; }

        [BsonElement("defaultGstRatePercent")]
        public decimal? DefaultGstRatePercent { get; set; }

        // Atomically incremented via FindOneAndUpdate($inc) by InvoiceService when an
        // invoice is sent — ReturnDocument.Before gives the number to use, leaving this
        // field already advanced for the next call. Starts at 1 so the first invoice is
        // "...-0001", not "...-0000".
        [BsonElement("nextSequence")]
        public int NextSequence { get; set; } = 1;

        public string FormatInvoiceNo(int sequence) =>
            $"{InvoiceNoPrefix}-{sequence.ToString("D" + Math.Max(1, NumberPadding))}";
    }
}
