using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Auth
{
    [BsonIgnoreExtraElements]
    public class UserModel : AuditableEntity
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

        [BsonElement("mobile")]
        [BsonRequired]
        public string Mobile { get; set; }

        [BsonElement("roleId")]
        public string RoleId { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("mustChangePassword")]
        public bool MustChangePassword { get; set; } = false;

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [BsonElement("resetToken")]
        public string? ResetToken { get; set; }

        [BsonElement("resetTokenExpiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        /// <summary>
        /// Plaintext password supplied on create, hashed into PasswordHash by the service layer, never persisted.
        /// </summary>
        [BsonIgnore]
        public string Password { get; set; } = string.Empty;

        public void ApplyEditableFields(UserModel updateModel)
        {
            Email = updateModel.Email;
            FirstName = updateModel.FirstName;
            LastName = updateModel.LastName;
            Mobile = updateModel.Mobile;
            RoleId = updateModel.RoleId;
            IsActive = updateModel.IsActive;
        }
    }
}