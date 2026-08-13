namespace Eduflex.DTOs.Task
{
    public class TaskItemFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }

        // "New" | "Processing" | "Completed" | null (any status)
        public string? Status { get; set; }

        // "Completed" for the Active tab (New + Processing) — see TaskItemFilter.
        public string? ExcludeStatus { get; set; }

        public string? EnrolmentId { get; set; }
        public string? EnquiryId { get; set; }
        public string? ApplicationId { get; set; }
        public string? FinancialRecordId { get; set; }
        public string? MigrationCaseId { get; set; }
    }
}
