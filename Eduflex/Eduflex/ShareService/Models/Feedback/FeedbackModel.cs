using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Feedback
{
    [BsonIgnoreExtraElements] // this attribute tells MongoDB to ignore any extra elements in the document that are not mapped to properties in the class
    public class FeedbackModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("photoData")]
        public string PhotoData { get; set; }   // base64-encoded thumbnail bytes

        [BsonElement("photoContentType")]
        public string PhotoContentType { get; set; }   // e.g. "image/jpeg"

        [BsonElement("courseName")]
        public string CourseName { get; set; }

        [BsonElement("comment")]
        public string Comment { get; set; }
    }
}