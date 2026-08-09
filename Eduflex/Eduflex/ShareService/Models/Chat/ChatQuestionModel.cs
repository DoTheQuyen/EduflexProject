using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Chat
{
    public class ChatQuestionModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("question")]
        public string Question { get; set; }

        [BsonElement("answer")]
        public string Answer { get; set; }

        [BsonElement("normalizedQuestion")]
        public string NormalizedQuestion { get; set; }
    }
}