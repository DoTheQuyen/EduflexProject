namespace Eduflex.API.DTOs
{
    public class CreateApplicationDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
    }
}