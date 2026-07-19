namespace Eduflex.API.DTOs
{
    public class EducationDto
    {
        public string Institution { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int YearCompleted { get; set; }
    }
}