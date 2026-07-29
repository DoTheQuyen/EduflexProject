namespace Eduflex.DTOs.Enrolment
{
    public class VisaProcessStepDto
    {
        public string Key { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new();
        public string? CompletedByName { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class SaveVisaStepDto
    {
        public Dictionary<string, string> Fields { get; set; } = new();
    }
}
