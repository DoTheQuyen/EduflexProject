using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Application
{
    public class ApplicationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("studentId")]
        public string StudentId { get; set; }

        [BsonElement("studentName")]
        public string StudentName { get; set; }

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

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
