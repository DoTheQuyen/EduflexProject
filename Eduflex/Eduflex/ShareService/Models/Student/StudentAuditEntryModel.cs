using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Student
{
    public class StudentAuditEntryModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("performedByUserId")]
        public string? PerformedByUserId { get; set; }

        [BsonElement("performedByName")]
        public string PerformedByName { get; set; } = string.Empty;

        [BsonElement("performedAt")]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public static StudentAuditEntryModel Create(string description, string? userId, string userName) => new()
        {
            Description = description,
            PerformedByUserId = userId,
            PerformedByName = userName
        };
    }
}
