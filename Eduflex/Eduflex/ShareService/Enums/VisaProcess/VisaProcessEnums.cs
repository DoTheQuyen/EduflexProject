using System.Text.Json.Serialization;

namespace ShareService.Enums.VisaProcess
{
    // Same shape as ShareService.Enums.DynamicForms.TemplateStatus — deactivated templates
    // are never hard-deleted, since enrolments may reference them by TemplateId even after
    // a template is retired. Named VisaTemplateStatus (not just TemplateStatus) to avoid
    // colliding with ShareService.Enums.DynamicForms.TemplateStatus wherever both
    // namespaces are in scope (e.g. MigrationCaseService, which uses both a
    // VisaProcessTemplate and a DynamicFormTemplate).
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VisaTemplateStatus
    {
        Active = 1,
        Inactive = 2
    }

    // Deliberately the same small set as the mockup's field types — one generic step-field
    // renderer on the frontend, no per-step bespoke controls.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StepFieldInputType
    {
        Text = 1,
        Date = 2,
        Number = 3,
        Select = 4,
        YesNo = 5
    }

    // Closed vocabulary, deliberately not a rule engine/DSL — see docs/09 §C.2. Covers every
    // precondition found in EnrolmentService.CompleteVisaStepAsync today plus the extended
    // step inventory in docs/09 Part D/F. Add a new value only when a genuinely new shape is
    // needed, not pre-emptively.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StepPreconditionType
    {
        PriorStepFieldNotEmpty = 1,
        CourseApplicationFinalized = 2,
        FieldValueIn = 3,
        AllPriorEvidenceUploaded = 4
    }
}
