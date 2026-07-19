using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DBMigration.Models
{
    public class MigrationRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("migrationId")]
        public string MigrationId { get; set; } // e.g., "001_AddApplicationsIndexes"

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("appliedAt")]
        public DateTime AppliedAt { get; set; }

        [BsonElement("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [BsonElement("success")]
        public bool Success { get; set; }

        [BsonElement("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}