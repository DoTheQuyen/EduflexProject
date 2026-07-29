namespace Eduflex.DTOs.Financial
{
    public class FinancialRecordFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? SearchTerm { get; set; }
    }
}
