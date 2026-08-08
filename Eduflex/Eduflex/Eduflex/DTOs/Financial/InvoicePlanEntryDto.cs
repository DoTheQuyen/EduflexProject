namespace Eduflex.DTOs.Financial
{
    public class InvoicePlanEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime? IntakeDate { get; set; }
        public DateTime ClaimDate { get; set; }
        public string Status { get; set; } = "Planned";
        public string? LinkedInvoiceId { get; set; }
        public string? SkipReason { get; set; }
        public bool IsManual { get; set; }
    }

    public class UpdatePlanEntryDateDto
    {
        public DateTime ClaimDate { get; set; }
    }

    public class SkipPlanEntryDto
    {
        public string? Reason { get; set; }
    }

    public class AddManualPlanEntryDto
    {
        public DateTime ClaimDate { get; set; }
    }
}
