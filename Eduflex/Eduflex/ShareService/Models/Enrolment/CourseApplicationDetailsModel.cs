namespace ShareService.Models.Enrolment
{
    // Plain carrier for UpdateCourseApplicationDetailsAsync — not a Mongo document, just
    // the editable-field subset of CourseApplicationModel (excludes Id/Status/CreatedAt/
    // TuitionFee's snapshot-on-add semantics/StatusUpdated*, which have their own
    // dedicated mutation paths).
    public class CourseApplicationDetailsModel
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
}
