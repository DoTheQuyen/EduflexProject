using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Education
{
    public class EducationModel
    {
        [BsonElement("institution")]
        public string Institution { get; set; }

        [BsonElement("qualification")]
        public string Qualification { get; set; }

        [BsonElement("yearCompleted")]
        public int YearCompleted { get; set; }
    }
}
