using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models
{
    public class CreateApplicationModel
    {
        [BsonElement("studentId")]
        public string StudentId { get; set; } = string.Empty;

        [BsonElement("StudentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("Details")]
        public string Details { get; set; } = string.Empty;

        [BsonElement("ApplicationType")]
        public string ApplicationType { get; set; } = string.Empty;
    }
}