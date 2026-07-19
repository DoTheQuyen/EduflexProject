using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Auth
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("email")]
        [BsonRequired]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        [BsonRequired]
        public string PasswordHash { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("role")]
        public string Role { get; set; } = "Student";

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }
    }
}