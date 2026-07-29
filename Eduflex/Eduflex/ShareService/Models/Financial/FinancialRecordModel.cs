using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Financial
{
    // 1:1 with an EnrolmentModel — auto-created exactly once, when that enrolment's VISA
    // Outcome step is completed with a "Granted" result (see
    // EnrolmentService.CompleteVisaStepAsync -> IFinancialRecordService.CreateForEnrolmentIfNotExistsAsync).
    [BsonIgnoreExtraElements]
    public class FinancialRecordModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("enrolmentId")]
        public string EnrolmentId { get; set; } = string.Empty;

        [BsonElement("educationPartnerId")]
        public string? EducationPartnerId { get; set; }

        [BsonElement("businessPartnerId")]
        public string? BusinessPartnerId { get; set; }

        // Snapshots taken at creation time — later rate changes on the Course/Business
        // Partner shouldn't silently reshape an already-created financial record.
        [BsonElement("courseCommissionRate")]
        public decimal CourseCommissionRate { get; set; }

        [BsonElement("businessPartnerCommissionRate")]
        public decimal BusinessPartnerCommissionRate { get; set; }

        [BsonElement("totalTuition")]
        public decimal TotalTuition { get; set; }

        // CourseCommissionRate% x BusinessPartnerCommissionRate% x TotalTuition when a
        // business partner is linked, or just CourseCommissionRate% x TotalTuition for a
        // direct Eduflex-EducationPartner contract.
        [BsonElement("expectedCommission")]
        public decimal ExpectedCommission { get; set; }

        [BsonElement("extraCommissionAdjustments")]
        public List<CommissionAdjustmentModel> ExtraCommissionAdjustments { get; set; } = new();

        [BsonElement("invoicePlan")]
        public List<InvoicePlanEntryModel> InvoicePlan { get; set; } = new();

        [BsonElement("invoices")]
        public List<InvoiceModel> Invoices { get; set; } = new();

        [BsonElement("communications")]
        public List<FinancialCommunicationModel> Communications { get; set; } = new();

        [BsonElement("auditTrail")]
        public List<FinancialAuditEntryModel> AuditTrail { get; set; } = new();
    }
}
