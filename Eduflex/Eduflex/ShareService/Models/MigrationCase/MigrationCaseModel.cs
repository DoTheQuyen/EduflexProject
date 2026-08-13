using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Address;
using ShareService.Models.Common;
using ShareService.Models.Enrolment;
using ShareService.Models.VisaProcess;

namespace ShareService.Models.MigrationCase
{
    // A generic, category-agnostic case entity — see
    // docs/09-visa-process-config-module-design.md Part G item 6. Deliberately its own
    // top-level collection, independent of EnrolmentModel: a Skilled/Partner/Parent/
    // Protection case has no course, no provider, no student concept, so forcing it onto
    // Enrolment's shape would mean a pile of always-null Enrolment-only fields. A Student
    // category case can equally be started here without needing an Enrolment record at
    // all — this collection and the existing Enrolment/VISA-tab system are intentionally
    // two separate, non-interacting things for now (see docs §Part G for the open question
    // on whether/how they should eventually relate).
    public class MigrationCaseModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("ownerUserId")]
        public string OwnerUserId { get; set; } = string.Empty;

        // Display code, e.g. "MIG-2026-00001" — generated at creation, never reused.
        [BsonElement("caseReference")]
        public string CaseReference { get; set; } = string.Empty;

        [BsonElement("templateId")]
        public string TemplateId { get; set; } = string.Empty;

        // The template's Version at the moment this case snapshotted it — for
        // audit/debugging only (docs §G.5), no diff/compare UI built against it yet.
        [BsonElement("templateVersion")]
        public int TemplateVersion { get; set; }

        // Copied from the template at creation time, purely so list/filter screens don't
        // need to join against VisaProcessTemplates for every row.
        [BsonElement("templateName")]
        public string TemplateName { get; set; } = string.Empty;

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        // The case's primary contact. Deliberately just a name/email/mobile, not a
        // first-class "Party" model — a second party (e.g. a partner-visa sponsor) is
        // captured as ordinary step Fields on whichever step the template defines for it
        // (see the SponsorEligibilityCheck-style steps in docs/09 Part F.3), not as a
        // structured field here. See docs §G.7 for why that's deferred rather than solved.
        [BsonElement("primaryContactName")]
        public string PrimaryContactName { get; set; } = string.Empty;

        [BsonElement("primaryContactEmail")]
        public string? PrimaryContactEmail { get; set; }

        [BsonElement("primaryContactMobile")]
        public string? PrimaryContactMobile { get; set; }

        [BsonElement("notes")]
        public string? Notes { get; set; }

        // ----- Customer Info (its own tab, editable any time — see
        // MigrationCaseService.UpdateCustomerInfoAsync). Reuses AddressModel/
        // EmergencyContactModel from ShareService.Models.Address/.Enrolment directly rather
        // than forking near-identical classes — this is pure data-shape reuse (no call into
        // EnrolmentService, no functional coupling), the "zero shared code paths" boundary
        // this module otherwise keeps is about behavior, not plain value objects. All
        // optional: PrimaryContactName/Email/Mobile above are captured at case creation and
        // are enough to identify the case; these fill in once staff complete the Customer
        // Info tab, unlike Enrolment where the equivalent (StudentInfo) is captured before
        // the record even exists. -----
        [BsonElement("middleName")]
        public string? MiddleName { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [BsonElement("gender")]
        public string? Gender { get; set; }

        [BsonElement("nationality")]
        public string? Nationality { get; set; }

        [BsonElement("passportNumber")]
        public string? PassportNumber { get; set; }

        [BsonElement("hometownAddress")]
        public AddressModel? HometownAddress { get; set; }

        [BsonElement("currentAddress")]
        public AddressModel? CurrentAddress { get; set; }

        [BsonElement("emergencyContact")]
        public EmergencyContactModel? EmergencyContact { get; set; }

        // Active | Completed | Withdrawn — deliberately a small, generic, category-agnostic
        // set (not EnrolmentEnums' Student-specific vocabulary). Completed is set
        // automatically when the last enabled step completes (MigrationCaseService); there
        // is no per-step "SetsCaseStatusTo" mapping — the generic case has nothing
        // equivalent to Enrolment's separate downstream consumers (FinancialRecord,
        // Application status) that made that mapping necessary there.
        [BsonElement("status")]
        public string Status { get; set; } = "Active";

        [BsonElement("steps")]
        public List<MigrationCaseStepModel> Steps { get; set; } = new();

        [BsonElement("documents")]
        public List<MigrationCaseDocumentModel> Documents { get; set; } = new();

        // Reuses EnrolmentCommunicationModel directly (ShareService.Models.Enrolment) —
        // same pure data-shape reuse as AddressModel/EmergencyContactModel above, not a
        // functional coupling to EnrolmentService. See
        // MigrationCaseService.SendCommunicationAsync.
        [BsonElement("communications")]
        public List<EnrolmentCommunicationModel> Communications { get; set; } = new();

        // Reuses EnrolmentFormResponseModel directly (ShareService.Models.Enrolment) — same
        // pure data-shape reuse as Communications above. CreateFromTemplate/RenderToHtml
        // (ShareService.Mapping.EnrolmentFormResponseMappingExtension) are already fully
        // generic extension methods with no Enrolment-specific coupling, so they're reused
        // as-is too — see MigrationCaseService's Dynamic Forms region. Unlike Enrolment,
        // there is no separate student-self-service submit path: a Migration Case contact
        // has no portal account, so staff request AND fill in the form themselves via
        // MigrationCaseService.SaveFormAnswersAsync.
        [BsonElement("formResponses")]
        public List<EnrolmentFormResponseModel> FormResponses { get; set; } = new();

        [BsonElement("auditTrail")]
        public List<MigrationCaseAuditEntryModel> AuditTrail { get; set; } = new();
    }

    // The one step every case gets regardless of template — "did we actually consult with
    // this person and decide to proceed" gates every templated step that follows, the same
    // way Enrolment's StudentInfo/EnrolmentForm always come first. Kept outside any one
    // template (rather than something each template author has to remember to add) since
    // every category needs it. Deliberately NOT auto-completed at case creation the way
    // Enrolment's first two steps are — MigrationCaseService.CreateCaseAsync's own creation
    // form is much lighter than the Enrolment wizard, so this starts Draft and is the first
    // thing staff fill in right after starting the case.
    public static class MigrationCaseConsultationStep
    {
        public const string Key = "Consultation";

        public static MigrationCaseStepModel Build()
        {
            return new MigrationCaseStepModel
            {
                Key = Key,
                Order = 0,
                Label = "Consultation",
                Description = "Booking + course counselling outcome — the first step of every case.",
                Phase = "Consultation",
                CanReopen = true,
                Status = "Draft",
                FieldsSnapshot = new List<StepFieldDefinitionModel>
                {
                    new() { FieldKey = "consultationDate", Label = "Consultation date", InputType = "Date", IsRequired = true },
                    new() { FieldKey = "consultant", Label = "Consultant", InputType = "Text", IsRequired = false },
                    new() { FieldKey = "notes", Label = "Notes", InputType = "Text", IsRequired = false },
                    new()
                    {
                        FieldKey = "outcome", Label = "Outcome", InputType = "Select", IsRequired = true,
                        Options = new List<string> { "Proceed", "Refer", "Decline" }
                    }
                }
                // No PreconditionsSnapshot — completing this step (any outcome value, since
                // "outcome" is simply required) unlocks the next one. Gating the rest of the
                // process specifically on outcome == "Proceed" would need conditional-unlock
                // logic CompleteStepAsync doesn't have (it always advances to the immediate
                // next step on completion) — not built, since nothing has asked for it yet;
                // a "Decline"d consultation is a business-process convention for now, not a
                // system-enforced dead end.
            };
        }
    }

    // The per-case snapshot of one VisaProcessStepDefinitionModel, frozen at case-creation
    // time — same reasoning as EnrolmentFormResponseModel.QuestionsSnapshot: later template
    // edits must never retroactively change an in-flight case.
    public class MigrationCaseStepModel
    {
        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        [BsonElement("order")]
        public int Order { get; set; }

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("phase")]
        public string? Phase { get; set; }

        [BsonElement("canReopen")]
        public bool CanReopen { get; set; }

        [BsonElement("practitionerTagId")]
        public string? PractitionerTagId { get; set; }

        [BsonElement("fieldsSnapshot")]
        public List<StepFieldDefinitionModel> FieldsSnapshot { get; set; } = new();

        [BsonElement("requiredEvidenceCategoriesSnapshot")]
        public List<string> RequiredEvidenceCategoriesSnapshot { get; set; } = new();

        [BsonElement("preconditionsSnapshot")]
        public List<StepPreconditionModel> PreconditionsSnapshot { get; set; } = new();

        [BsonElement("hintsSnapshot")]
        public List<ProcessStepHintModel> HintsSnapshot { get; set; } = new();

        // Locked | Draft | Complete — same three-state shape as Enrolment's
        // VisaProcessStepModel.Status.
        [BsonElement("status")]
        public string Status { get; set; } = "Locked";

        // The staff-entered answers, keyed by FieldsSnapshot[].FieldKey.
        [BsonElement("fields")]
        public Dictionary<string, string> Fields { get; set; } = new();

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("completedByUserId")]
        public string? CompletedByUserId { get; set; }

        [BsonElement("completedByName")]
        public string? CompletedByName { get; set; }
    }

    // Mirrors EnrolmentDocumentModel minus the CourseApplicationId linkage (there's no
    // course-application concept in a generic case).
    public class MigrationCaseDocumentModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("category")]
        public string? Category { get; set; }

        [BsonElement("note")]
        public string? Note { get; set; }

        [BsonElement("url")]
        public string Url { get; set; } = string.Empty;

        [BsonElement("contentType")]
        public string? ContentType { get; set; }

        [BsonElement("sizeBytes")]
        public long SizeBytes { get; set; }

        [BsonElement("uploadedByUserId")]
        public string? UploadedByUserId { get; set; }

        [BsonElement("uploadedByName")]
        public string UploadedByName { get; set; } = string.Empty;

        [BsonElement("uploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public class MigrationCaseAuditEntryModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("performedByUserId")]
        public string? PerformedByUserId { get; set; }

        [BsonElement("performedByName")]
        public string PerformedByName { get; set; } = string.Empty;

        [BsonElement("performedAt")]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public static MigrationCaseAuditEntryModel Create(string description, string? userId, string userName) => new()
        {
            Description = description,
            PerformedByUserId = userId,
            PerformedByName = userName,
            PerformedAt = DateTime.UtcNow
        };
    }
}
