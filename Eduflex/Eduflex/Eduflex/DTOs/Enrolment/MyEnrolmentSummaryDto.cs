namespace Eduflex.DTOs.Enrolment
{
    public class MyEnrolmentSummaryDto
    {
        public string EnrolmentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CourseApplicationCount { get; set; }
        public string? FinalizedCourseApplicationName { get; set; }
    }
}
