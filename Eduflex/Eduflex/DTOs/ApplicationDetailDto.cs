namespace Eduflex.API.DTOs
{
    public class ApplicationDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateApplied { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
    }
}