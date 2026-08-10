namespace Eduflex.DTOs.StudentPaymentPlan
{
    public class StudentPaymentPlanEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string Label { get; set; } = string.Empty;
        public int InstalmentNumber { get; set; }
        public int TotalInstalments { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? LinkedInvoiceId { get; set; }
        public string? SkipReason { get; set; }
        public bool IsManual { get; set; }
    }

    public class GenerateStudentPaymentPlanDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public decimal TotalAmount { get; set; }
        public int InstalmentCount { get; set; }
        public DateTime FirstDueDate { get; set; }
        public int IntervalMonths { get; set; } = 6;
    }

    public class AddManualStudentPlanEntryDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class UpdateStudentPlanEntryDateDto
    {
        public DateTime DueDate { get; set; }
    }

    public class SkipStudentPlanEntryDto
    {
        public string? Reason { get; set; }
    }
}
