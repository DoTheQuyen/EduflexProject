namespace Eduflex.DTOs.Task
{
    // Deliberately excludes AssigneeUserId (changing owner only ever happens through
    // POST {id}/reassign, which records a mandatory note) and Status (only ever changed
    // through PUT {id}/status, which records a system timeline entry) — this endpoint is
    // the assigner editing the task's own details, nothing else.
    public class UpdateTaskItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDateTime { get; set; }
        public string? EnrolmentId { get; set; }
        public string? EnquiryId { get; set; }
        public string? ApplicationId { get; set; }
        public string? FinancialRecordId { get; set; }
        public string? MigrationCaseId { get; set; }
    }
}
