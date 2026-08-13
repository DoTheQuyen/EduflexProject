using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.VisaProcess;
using ShareService.Models.Common;

namespace ShareService.Models.VisaProcess
{
    // Admin-authored process template — see docs/09-visa-process-config-module-design.md §C.2.
    // Not yet consumed by EnrolmentService/the VISA Process tab (those still run on the
    // compiled VisaProcessStepKeys/VisaProcessStepModel.CreateDefault in
    // ShareService/Models/Enrolment/VisaProcessStepModel.cs) — this is the standalone
    // authoring side of the module, mirroring how DynamicFormTemplateModel exists
    // independently of any one enrolment.
    public class VisaProcessTemplateModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        // Free string, not an enum — keeps future markets (NZ/CA/UK/US) open without a
        // code change. "AU" for every template today.
        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;

        // Free string, not an enum — e.g. "Student", "GraduateWork485",
        // "SkilledIndependent189", "Partner", "ParentSponsor", "Protection". A template is
        // really keyed on (Country, Category), not Country alone.
        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = VisaTemplateStatus.Active.ToString();

        // One default template per (Country, Category) — enforced in
        // VisaProcessTemplateService, not at the database level.
        [BsonElement("isDefaultForCountry")]
        public bool IsDefaultForCountry { get; set; }

        // Bumped on every published edit. Not yet consumed by anything (no instance
        // snapshotting exists until the EnrolmentService integration phase), but kept from
        // day one so early templates don't need a schema change later.
        [BsonElement("version")]
        public int Version { get; set; } = 1;

        [BsonElement("steps")]
        public List<VisaProcessStepDefinitionModel> Steps { get; set; } = new();
    }

    public class VisaProcessStepDefinitionModel
    {
        // Free string/slug, not a compiled const — e.g. "ApplyOffer", "OshcMonitoring".
        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        [BsonElement("order")]
        public int Order { get; set; }

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        // Free string grouping for UI sectioning, e.g. "Application", "Compliance" — not
        // enforced, display-only.
        [BsonElement("phase")]
        public string? Phase { get; set; }

        // The per-business/culture on-off toggle.
        [BsonElement("enabled")]
        public bool Enabled { get; set; } = true;

        [BsonElement("canReopen")]
        public bool CanReopen { get; set; } = true;

        // Optional FK into PractitionerTagModel — staffing/routing metadata only, no
        // enforcement anywhere. Null = no tag.
        [BsonElement("practitionerTagId")]
        public string? PractitionerTagId { get; set; }

        [BsonElement("fields")]
        public List<StepFieldDefinitionModel> Fields { get; set; } = new();

        [BsonElement("requiredEvidenceCategories")]
        public List<string> RequiredEvidenceCategories { get; set; } = new();

        [BsonElement("preconditions")]
        public List<StepPreconditionModel> Preconditions { get; set; } = new();

        // Name of an ShareService.Enums.Roles.EnrolmentEnums member (e.g. "Offer", "Coe"),
        // or null for no status side-effect on completion. Kept as a plain string rather
        // than a hard enum reference so this module doesn't need to depend on the
        // Enrolment-specific enum's namespace/assembly shape.
        [BsonElement("setsEnrolmentStatusTo")]
        public string? SetsEnrolmentStatusTo { get; set; }

        [BsonElement("hints")]
        public List<ProcessStepHintModel> Hints { get; set; } = new();
    }

    public class StepFieldDefinitionModel
    {
        // Dictionary key inside an instance step's Fields bag once this module is wired
        // into EnrolmentService — see docs/09 §C.2/§C.4.
        [BsonElement("fieldKey")]
        public string FieldKey { get; set; } = string.Empty;

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("inputType")]
        public string InputType { get; set; } = StepFieldInputType.Text.ToString();

        // Only populated for InputType == Select.
        [BsonElement("options")]
        public List<string> Options { get; set; } = new();

        [BsonElement("isRequired")]
        public bool IsRequired { get; set; }
    }

    public class StepPreconditionModel
    {
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        // For PriorStepFieldNotEmpty — which earlier step's Fields bag to check.
        [BsonElement("sourceStepKey")]
        public string? SourceStepKey { get; set; }

        // For PriorStepFieldNotEmpty / FieldValueIn — which field key to check.
        [BsonElement("fieldKey")]
        public string? FieldKey { get; set; }

        // For FieldValueIn — the set of values the field is allowed to hold.
        [BsonElement("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        // Human-readable explanation shown in the Process Designer's step detail panel —
        // e.g. "EnrolmentForm.fields.invoiceId must be set". Not evaluated, display only.
        [BsonElement("detail")]
        public string? Detail { get; set; }
    }

    // Append-only "experience sharing from senior staff" entry — same shape family as
    // EnrolmentAuditEntryModel/EnrolmentCommunicationModel elsewhere in this codebase.
    public class ProcessStepHintModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("text")]
        public string Text { get; set; } = string.Empty;

        [BsonElement("authorUserId")]
        public string? AuthorUserId { get; set; }

        [BsonElement("authorName")]
        public string? AuthorName { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Lets a senior mark their best tip as the one shown collapsed-by-default.
        [BsonElement("pinned")]
        public bool Pinned { get; set; }
    }
}
