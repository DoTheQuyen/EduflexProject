using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Financial
{
    public class FinancialCommunicationModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("templateKey")]
        public string? TemplateKey { get; set; }

        [BsonElement("toEmail")]
        public string ToEmail { get; set; } = string.Empty;

        // EducationPartner | BusinessPartner — no "Student" option on the Financial
        // module's Communication tab, unlike Enrolment's.
        [BsonElement("recipientType")]
        public string RecipientType { get; set; } = "EducationPartner";

        [BsonElement("subject")]
        public string Subject { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        // Optional — set when this email references/attaches a specific generated
        // invoice's download link.
        [BsonElement("relatedInvoiceId")]
        public string? RelatedInvoiceId { get; set; }

        [BsonElement("sentByUserId")]
        public string? SentByUserId { get; set; }

        [BsonElement("sentByName")]
        public string SentByName { get; set; } = string.Empty;

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
