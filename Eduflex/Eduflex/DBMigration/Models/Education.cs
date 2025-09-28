using MongoDB.Bson.Serialization.Attributes;

namespace DBMigration.Models
{
    public class Education
    {
        [BsonElement("institution")]
        public string Institution { get; set; }

        [BsonElement("qualification")]
        public string Qualification { get; set; }

        [BsonElement("yearCompleted")]
        public int YearCompleted { get; set; }
    }
}
