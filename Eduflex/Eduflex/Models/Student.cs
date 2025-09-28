using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.API.Models
{
    public class Student
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("nationality")]
        public string Nationality { get; set; }

        [BsonElement("passportNumber")]
        public string PassportNumber { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("address")]
        public Address Address { get; set; }

        [BsonElement("educationBackground")]
        public List<Education> EducationBackground { get; set; } = new List<Education>();

        [BsonElement("applications")]
        public List<Application> Applications { get; set; } = new List<Application>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}