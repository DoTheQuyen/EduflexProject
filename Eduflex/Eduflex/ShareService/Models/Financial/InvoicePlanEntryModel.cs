using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Financial
{
    // Per-enrolment invoice-request/claim schedule, derived from the course's own intake
    // calendar (see FinancialRecordService.BuildIntakePlan): one entry per intake cycle
    // that falls within the course duration, due 45 days after that intake. Regenerable,
    // and each entry is individually editable/skippable — not purely derived-and-frozen
    // like the original single-entry version.
    public class InvoicePlanEntryModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // The course intake this claim is tied to — null for manually-added claims that
        // don't correspond to a real intake cycle.
        [BsonElement("intakeDate")]
        public DateTime? IntakeDate { get; set; }

        // Editable — defaults to IntakeDate + 45 days when generated, but staff can move it.
        [BsonElement("claimDate")]
        public DateTime ClaimDate { get; set; }

        // Planned | Invoiced | Skipped
        [BsonElement("status")]
        public string Status { get; set; } = "Planned";

        [BsonElement("linkedInvoiceId")]
        public string? LinkedInvoiceId { get; set; }

        [BsonElement("skipReason")]
        public string? SkipReason { get; set; }

        // True for claims added via "+ Add manual claim" rather than generated from the
        // course's intake calendar — Regenerate leaves these untouched.
        [BsonElement("isManual")]
        public bool IsManual { get; set; }
    }
}
