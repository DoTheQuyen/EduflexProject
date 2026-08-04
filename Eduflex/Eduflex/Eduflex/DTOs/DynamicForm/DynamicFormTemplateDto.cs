namespace Eduflex.DTOs.DynamicForm
{
    public class FormQuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public string AnswerType { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public bool IsRequired { get; set; }
        public int? MaxLength { get; set; }
        public bool OptionsHorizontal { get; set; }
    }

    public class DynamicFormTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BoundStepKey { get; set; }
        public List<FormQuestionDto> Questions { get; set; } = new();
    }

    public class SaveDynamicFormTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BoundStepKey { get; set; }
        public List<FormQuestionDto> Questions { get; set; } = new();
    }

    public class SetDynamicFormTemplateStatusDto
    {
        public bool IsActive { get; set; }
    }
}
