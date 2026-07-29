namespace Eduflex.DTOs.Financial
{
    public class FinancialRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        // Resolved for list-display purposes only (e.g. the Finance list page) — not
        // stored on the model itself, since the enrolment record is the source of truth.
        public string StudentName { get; set; } = string.Empty;
        public string EnrolmentStatus { get; set; } = string.Empty;
        public string? EducationPartnerId { get; set; }
        public string? BusinessPartnerId { get; set; }
        public decimal CourseCommissionRate { get; set; }
        public decimal BusinessPartnerCommissionRate { get; set; }
        public decimal TotalTuition { get; set; }
        public decimal ExpectedCommission { get; set; }
        public List<CommissionAdjustmentDto> ExtraCommissionAdjustments { get; set; } = new();
        public List<InvoicePlanEntryDto> InvoicePlan { get; set; } = new();
        public List<InvoiceDto> Invoices { get; set; } = new();
        public List<FinancialCommunicationDto> Communications { get; set; } = new();
        public List<FinancialAuditEntryDto> AuditTrail { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
