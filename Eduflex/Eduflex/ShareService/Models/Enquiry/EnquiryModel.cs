using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enquiry
{
    public class EnquiryModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("middleName")]
        public string? MiddleName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("mobile")]
        public string Mobile { get; set; }

        [BsonElement("enquiry")]
        public string Enquiry { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "New";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
