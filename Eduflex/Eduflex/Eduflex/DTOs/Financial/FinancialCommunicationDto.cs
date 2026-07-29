namespace Eduflex.DTOs.Financial
{
    public class FinancialCommunicationDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? RelatedInvoiceId { get; set; }
        public string SentByName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class SendFinancialCommunicationDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string? RelatedInvoiceId { get; set; }
    }
}
