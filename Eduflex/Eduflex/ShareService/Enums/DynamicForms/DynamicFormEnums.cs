using System.Text.Json.Serialization;

namespace ShareService.Enums.DynamicForms
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TemplateStatus
    {
        Active = 1,
        Inactive = 2
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnswerType
    {
        RichText = 1,
        YesNo = 2,
        SingleSelect = 3,
        MultiSelect = 4
    }

    // Requesting -> Draft -> Responded is the normal path. Withdrawn ends it early.
    // Staff "allow edit" reverses Responded back to Draft rather than adding a 5th
    // status — see docs/08-dynamic-form-module-design.md §2.3.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResponseStatus
    {
        Requesting = 1,
        Draft = 2,
        Responded = 3,
        Withdrawn = 4,
        Archived = 5
    }

    public static class DynamicFormLimits
    {
        // Shared with the frontend's RICH_TEXT_MAX_LENGTH (models/dynamic-form.ts) —
        // keep both in sync if this changes.
        public const int RichTextAnswerMaxLength = 5000;
    }
}
