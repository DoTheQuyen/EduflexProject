namespace Eduflex.DTOs.Financial
{
    public class CommissionAdjustmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AddedByName { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class AddCommissionAdjustmentDto
    {
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
