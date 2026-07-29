namespace Eduflex.DTOs.Enrolment
{
    public class EnrolmentCommunicationDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = "Student";
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> AttachedDocumentIds { get; set; } = new();
        public string SentByName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class SendEnrolmentCommunicationDto
    {
        public string? TemplateKey { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = "Student";
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> AttachedDocumentIds { get; set; } = new();
    }
}
