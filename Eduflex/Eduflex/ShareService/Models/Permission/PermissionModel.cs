using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Permission
{
    [BsonIgnoreExtraElements]
    public class PermissionModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("moduleId")]
        [BsonRequired]
        public string ModuleId { get; set; }

        [BsonElement("action")]
        [BsonRepresentation(BsonType.String)]
        public PermissionAction Action { get; set; }

        [BsonElement("key")]
        [BsonRequired]
        public string Key { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }
    }
}
