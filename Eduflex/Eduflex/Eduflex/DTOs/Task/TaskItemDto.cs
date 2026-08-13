namespace Eduflex.DTOs.Task
{
    public class TaskItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssignerUserId { get; set; } = string.Empty;
        public string AssigneeUserId { get; set; } = string.Empty;
        public DateTime DueDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? EnrolmentId { get; set; }
        public string? EnquiryId { get; set; }
        public string? ApplicationId { get; set; }
        public string? FinancialRecordId { get; set; }
        public string? MigrationCaseId { get; set; }
        public List<TaskNoteDto> Notes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
