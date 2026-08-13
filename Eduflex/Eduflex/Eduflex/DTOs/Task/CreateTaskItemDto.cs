namespace Eduflex.DTOs.Task
{
    public class CreateTaskItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssigneeUserId { get; set; } = string.Empty;
        public DateTime DueDateTime { get; set; }
        public string? EnrolmentId { get; set; }
        public string? EnquiryId { get; set; }
        public string? ApplicationId { get; set; }
        public string? FinancialRecordId { get; set; }
        public string? MigrationCaseId { get; set; }
    }
}
