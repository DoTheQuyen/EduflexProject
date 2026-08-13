namespace Eduflex.DTOs.VisaProcess
{
    public class PractitionerTagDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Active { get; set; }
    }

    public class SavePractitionerTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Active { get; set; } = true;
    }

    public class SetPractitionerTagActiveDto
    {
        public bool IsActive { get; set; }
    }
}
