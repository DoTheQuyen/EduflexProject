namespace Eduflex.DTOs.Feedback
{
    public class CreateFeedbackDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhotoData { get; set; } = string.Empty;
        public string PhotoContentType { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}