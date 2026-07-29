using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enrolment
{
    public class EnrolmentCommunicationModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("templateKey")]
        public string? TemplateKey { get; set; }

        [BsonElement("toEmail")]
        public string ToEmail { get; set; } = string.Empty;

        // Student | EducationPartner | BusinessPartner
        [BsonElement("recipientType")]
        public string RecipientType { get; set; } = "Student";

        [BsonElement("subject")]
        public string Subject { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        [BsonElement("attachedDocumentIds")]
        public List<string> AttachedDocumentIds { get; set; } = new();

        [BsonElement("sentByUserId")]
        public string? SentByUserId { get; set; }

        [BsonElement("sentByName")]
        public string SentByName { get; set; } = string.Empty;

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
