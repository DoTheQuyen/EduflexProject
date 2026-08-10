using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.StudentPaymentPlan
{
    // Planned/Invoiced mirror InvoicePlanEntryModel's lifecycle (see ShareService.Models.
    // Financial.InvoicePlanEntryModel); Skipped likewise. There is no separate "Paid"
    // here — once an entry is Invoiced, its collection status is read live off the
    // linked Invoice (LinkedInvoiceId -> InvoiceModel.Status), the same join pattern
    // the Commission claim schedule already uses, so the two states never drift apart.
    public static class StudentPaymentPlanEntryStatuses
    {
        public const string Planned = "Planned";
        public const string Invoiced = "Invoiced";
        public const string Skipped = "Skipped";
    }

    // Top-level ledger of every student tuition/fee instalment, one document per
    // instalment — flat and indexable by EnrolmentId/DueDate/Status, unlike
    // InvoicePlanEntryModel which is embedded inside FinancialRecordModel. That
    // difference is deliberate: this is what the Action Queue and Accounts screens
    // query across the whole portfolio (hundreds of students, years of instalments),
    // and an embedded-array design can't be indexed or paged the same way. Commission
    // claims stay embedded because they're always read in the context of one
    // FinancialRecord; instalments are read across accounts, not just within one.
    public class StudentPaymentPlanEntryModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("enrolmentId")]
        public string EnrolmentId { get; set; } = string.Empty;

        // Snapshot of the student/course at plan-creation time, so Action Queue/Accounts
        // rows don't need to join back to Enrolment just to render a name — same
        // reasoning as InvoiceModel's StudentName/CourseName snapshot fields.
        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("courseName")]
        public string? CourseName { get; set; }

        // Short display label, e.g. "Term 2" — instalments aren't tied to a course
        // intake calendar the way commission claims are, so there's no equivalent to
        // InvoicePlanEntryModel.IntakeDate to derive a label from.
        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("instalmentNumber")]
        public int InstalmentNumber { get; set; }

        [BsonElement("totalInstalments")]
        public int TotalInstalments { get; set; }

        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = StudentPaymentPlanEntryStatuses.Planned;

        [BsonElement("linkedInvoiceId")]
        public string? LinkedInvoiceId { get; set; }

        [BsonElement("skipReason")]
        public string? SkipReason { get; set; }

        // True for instalments added one at a time via "+ Add instalment" rather than
        // the even-split generator (see StudentPaymentPlanService.GeneratePlanAsync) —
        // mirrors InvoicePlanEntryModel.IsManual.
        [BsonElement("isManual")]
        public bool IsManual { get; set; }

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonElement("createdByName")]
        public string CreatedByName { get; set; } = string.Empty;
    }
}
