using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Financial
{
    // Derived, per-enrolment invoice-request calendar — one planned entry, one month
    // after this enrolment's actual commencement date. Regenerated, not user-edited
    // directly; a real Invoice created against it flips its status and links back.
    public class InvoicePlanEntryModel
    {
        [BsonElement("plannedRequestDate")]
        public DateTime PlannedRequestDate { get; set; }

        // Planned | Invoiced | Skipped
        [BsonElement("status")]
        public string Status { get; set; } = "Planned";

        [BsonElement("linkedInvoiceId")]
        public string? LinkedInvoiceId { get; set; }
    }
}
