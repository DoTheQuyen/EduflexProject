namespace Eduflex.DTOs.Financial
{
    public class InvoicePlanEntryDto
    {
        public DateTime PlannedRequestDate { get; set; }
        public string Status { get; set; } = "Planned";
        public string? LinkedInvoiceId { get; set; }
    }
}
