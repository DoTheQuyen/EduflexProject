using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.API.Models
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
