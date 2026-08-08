namespace Eduflex.DTOs.Enrolment
{
    public class CourseApplicationDto
    {
        public string Id { get; set; } = string.Empty;
        public string EducationPartnerId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Intake { get; set; }
        public string? StudyMode { get; set; }
        public string? Campus { get; set; }
        public DateTime? CommencementDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public DateTime? ActualCommencementDate { get; set; }
        public string? Notes { get; set; }
        public decimal? TuitionFee { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? StatusUpdatedByName { get; set; }
        public DateTime? OfferAppliedDate { get; set; }
    }

    public class AddCourseApplicationDto
    {
        public string EducationPartnerId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
    }

    public class UpdateCourseApplicationDetailsDto
    {
        public string? Intake { get; set; }
        public string? StudyMode { get; set; }
        public string? Campus { get; set; }
        public DateTime? CommencementDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public DateTime? ActualCommencementDate { get; set; }
        public decimal? TuitionFee { get; set; }
        public string? Notes { get; set; }
    }

    public class SetCourseApplicationStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class SetMaxApplicationsOverrideDto
    {
        public int MaxApplications { get; set; }
    }
}
