using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Department
{
    [BsonIgnoreExtraElements]
    public class DepartmentModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        [BsonRequired]
        public string Name { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        // Self-referencing — makes the structure a tree instead of a flat list, so
        // nesting a new sub-department later needs no schema change.
        [BsonElement("parentDepartmentId")]
        public string? ParentDepartmentId { get; set; }

        [BsonElement("headUserId")]
        public string? HeadUserId { get; set; }

        [BsonElement("memberUserIds")]
        public List<string> MemberUserIds { get; set; } = new();
    }
}
