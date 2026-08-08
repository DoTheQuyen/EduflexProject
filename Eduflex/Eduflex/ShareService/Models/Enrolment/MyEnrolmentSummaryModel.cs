namespace ShareService.Models.Enrolment
{
    // Student-facing projection of an Enrolment — deliberately narrow. The student gets
    // "where am I" (Status) and "how many options are in play" (CourseApplicationCount),
    // never the full CourseApplications list — which ones were tried and withdrawn, staff
    // notes, etc. stay staff-only.
    public class MyEnrolmentSummaryModel
    {
        public string EnrolmentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CourseApplicationCount { get; set; }
        public string? FinalizedCourseApplicationName { get; set; }
    }
}
