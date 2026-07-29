using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enrolment
{
    /// <summary>
    /// Fixed, ordered set of keys for the VISA Process tab's step-gated workflow.
    /// Order matters here — it IS the unlock sequence enforced in EnrolmentService.
    /// </summary>
    public static class VisaProcessStepKeys
    {
        public const string StudentInfo = "StudentInfo";
        public const string EnrolmentForm = "EnrolmentForm";
        public const string ApplyOffer = "ApplyOffer";
        public const string CoeCompletion = "CoeCompletion";
        public const string VisaApplication = "VisaApplication";
        public const string VisaOutcome = "VisaOutcome";

        public static readonly string[] Ordered =
        {
            StudentInfo, EnrolmentForm, ApplyOffer, CoeCompletion, VisaApplication, VisaOutcome
        };

        // Evidence document Category associated with each step's upload zone. Only
        // enforced as a completion *requirement* for the four gated steps (ApplyOffer
        // onward) in CompleteVisaStepAsync — EnrolmentForm's "GS" entry is here purely so
        // the frontend can render its upload zone; that step is already Complete by the
        // time it's ever shown (auto-completed at enrolment creation), so the requirement
        // never actually gets checked for it.
        public static readonly IReadOnlyDictionary<string, string> RequiredEvidenceCategory = new Dictionary<string, string>
        {
            [EnrolmentForm] = "GS",
            [ApplyOffer] = "UniOffer",
            [CoeCompletion] = "CoE",
            [VisaApplication] = "VisaDraft",
            [VisaOutcome] = "VisaGranted"
        };
    }

    public class VisaProcessStepModel
    {
        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        // Locked | Draft | Complete
        [BsonElement("status")]
        public string Status { get; set; } = "Locked";

        // Free-form per-step field values (e.g. "coeNumber", "visaExpiryDate") — kept as a
        // dictionary rather than one typed class per step so new steps/fields don't need a
        // model change, matching this project's "avoid overengineering" convention.
        [BsonElement("fields")]
        public Dictionary<string, string> Fields { get; set; } = new();

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("completedByUserId")]
        public string? CompletedByUserId { get; set; }

        [BsonElement("completedByName")]
        public string? CompletedByName { get; set; }

        // Student Info + Enrolment Form are captured as part of creating the enrolment
        // itself, so they start Complete; Apply Offer is the first actionable step.
        public static List<VisaProcessStepModel> CreateDefault(string actingUserId, string actingUserName)
        {
            var now = DateTime.UtcNow;
            return VisaProcessStepKeys.Ordered.Select(key =>
            {
                var isAutoComplete = key == VisaProcessStepKeys.StudentInfo || key == VisaProcessStepKeys.EnrolmentForm;
                return new VisaProcessStepModel
                {
                    Key = key,
                    Status = isAutoComplete ? "Complete" : key == VisaProcessStepKeys.ApplyOffer ? "Draft" : "Locked",
                    CompletedAt = isAutoComplete ? now : null,
                    CompletedByUserId = isAutoComplete ? actingUserId : null,
                    CompletedByName = isAutoComplete ? actingUserName : null
                };
            }).ToList();
        }
    }
}
