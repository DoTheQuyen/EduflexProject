namespace Eduflex.DTOs.VisaProcess
{
    public class StepFieldDefinitionDto
    {
        public string FieldKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string InputType { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public bool IsRequired { get; set; }
    }

    public class StepPreconditionDto
    {
        public string Type { get; set; } = string.Empty;
        public string? SourceStepKey { get; set; }
        public string? FieldKey { get; set; }
        public List<string> AllowedValues { get; set; } = new();
        public string? Detail { get; set; }
    }

    public class ProcessStepHintDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Pinned { get; set; }
    }

    public class VisaProcessStepDefinitionDto
    {
        public string Key { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Phase { get; set; }
        public bool Enabled { get; set; }
        public bool CanReopen { get; set; }
        public string? PractitionerTagId { get; set; }
        public List<StepFieldDefinitionDto> Fields { get; set; } = new();
        public List<string> RequiredEvidenceCategories { get; set; } = new();
        public List<StepPreconditionDto> Preconditions { get; set; } = new();
        public string? SetsEnrolmentStatusTo { get; set; }
        public List<ProcessStepHintDto> Hints { get; set; } = new();
    }

    public class VisaProcessTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsDefaultForCountry { get; set; }
        public int Version { get; set; }
        public List<VisaProcessStepDefinitionDto> Steps { get; set; } = new();
    }

    public class SaveVisaProcessTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsDefaultForCountry { get; set; }
        public List<VisaProcessStepDefinitionDto> Steps { get; set; } = new();
    }

    public class SetVisaProcessTemplateStatusDto
    {
        public bool IsActive { get; set; }
    }
}
