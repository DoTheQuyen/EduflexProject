using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Application
{
    public class ApplicationModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("studentId")]
        public string StudentId { get; set; } = string.Empty;

        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("studentEmail")]
        public string StudentEmail { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("details")]
        public string Details { get; set; }

        [BsonElement("applicationType")]
        public string ApplicationType { get; set; }

        [BsonElement("dateApplied")]
        public DateTime DateApplied { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Pending";
    }
}
