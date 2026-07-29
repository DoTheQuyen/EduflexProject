using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Enrolment
{
    public class EmailTemplateModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("subject")]
        public string Subject { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        [BsonElement("isSystemDefault")]
        public bool IsSystemDefault { get; set; }

        public void ApplyEditableFields(EmailTemplateModel updateModel)
        {
            Name = updateModel.Name;
            Subject = updateModel.Subject;
            Body = updateModel.Body;
        }
    }
}
