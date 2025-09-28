using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models
{

    public class ApplicationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("studentId")]
        public string StudentId { get; set; } = string.Empty;

        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("dateApplied")]
        public DateTime DateApplied { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [BsonElement("details")]
        public string Details { get; set; } = string.Empty;

        [BsonElement("applicationType")]
        public string ApplicationType { get; set; } = string.Empty;
    }
}
