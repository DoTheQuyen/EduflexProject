using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enrolment
{
    public class EmergencyContactModel
    {
        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("relationship")]
        public string? Relationship { get; set; }

        [BsonElement("phone")]
        public string? Phone { get; set; }

        [BsonElement("email")]
        public string? Email { get; set; }
    }
}
