using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enrolment
{
    // A student can be pursuing several courses/universities at once under one
    // Enrolment record — each gets its own row here with its own lifecycle, independent
    // of the enrolment-wide VISA Process steps. When CoE Completion is reached, exactly
    // one of these must have been Finalized (via FinalizeCourseApplicationAsync), which
    // is also the moment every sibling entry here flips to Withdrawn.
    public static class CourseApplicationStatuses
    {
        public const string Init = "Init";
        public const string Applied = "Applied";
        public const string Offered = "Offered";
        public const string Finalized = "Finalized";
        public const string Withdrawn = "Withdrawn";
    }

    public class CourseApplicationModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("educationPartnerId")]
        public string EducationPartnerId { get; set; } = string.Empty;

        [BsonElement("courseId")]
        public string CourseId { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = CourseApplicationStatuses.Init;

        // ----- Same field set the Enrolment Form step used to carry once, at the
        // enrolment level, for a single course — now one copy per course application, so
        // the total count of "courses on this enrolment" is just this list's length, not
        // "one main course plus N applications". Editable via UpdateCourseApplicationDetailsAsync. -----
        [BsonElement("intake")]
        public string? Intake { get; set; }

        [BsonElement("studyMode")]
        public string? StudyMode { get; set; }

        [BsonElement("campus")]
        public string? Campus { get; set; }

        [BsonElement("commencementDate")]
        public DateTime? CommencementDate { get; set; }

        [BsonElement("expectedCompletionDate")]
        public DateTime? ExpectedCompletionDate { get; set; }

        [BsonElement("actualCommencementDate")]
        public DateTime? ActualCommencementDate { get; set; }

        [BsonElement("notes")]
        public string? Notes { get; set; }

        // Snapshotted from the Course catalog when added, not looked up live — so the
        // amount finance eventually uses doesn't silently drift if someone edits the
        // course's listed fee after this application was raised. Staff can still adjust
        // it directly on this entry if needed (not exposed as a general edit endpoint —
        // only ever set at creation for now, matching this project's "avoid
        // overengineering" convention until there's an actual need to change it later).
        [BsonElement("tuitionFee")]
        public decimal? TuitionFee { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("statusUpdatedAt")]
        public DateTime? StatusUpdatedAt { get; set; }

        [BsonElement("statusUpdatedByName")]
        public string? StatusUpdatedByName { get; set; }
    }
}
