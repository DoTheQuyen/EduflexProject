using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.CoursePromotion
{
    [BsonIgnoreExtraElements]
    public class CoursePromotionModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("courseName")]
        public string CourseName { get; set; }

        [BsonElement("universityName")]
        public string UniversityName { get; set; }

        [BsonElement("semester")]
        public string Semester { get; set; }

        [BsonElement("scholarshipLabel")]
        public string ScholarshipLabel { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("tuition")]
        public string Tuition { get; set; }

        [BsonElement("opportunities")]
        public string Opportunities { get; set; }

        [BsonElement("expiryDate")]
        public DateTime ExpiryDate { get; set; }

        [BsonElement("note")]
        public string Note { get; set; }

        [BsonElement("websiteUrl")]
        public string WebsiteUrl { get; set; }

        [BsonElement("isFeatured")]
        public bool IsFeatured { get; set; }

        [BsonElement("displayOrder")]
        public int DisplayOrder { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
