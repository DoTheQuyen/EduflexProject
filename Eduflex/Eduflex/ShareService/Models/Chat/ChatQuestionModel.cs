using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Chat;
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

        // Detected from the question text (e.g. "en"/"vi") — lets a future training-data
        // export group or pair up answers by language without re-inferring it later.
        [BsonElement("language")]
        public string Language { get; set; } = string.Empty;

        // Which downstream AI actually produced Answer (Gemini/Groq/OpenRouter).
        [BsonElement("provider")]
        public string Provider { get; set; } = string.Empty;

        // Curation gate for training-data export — every new record starts Unreviewed;
        // staff flip it to Approved/Rejected before it's ever used as a training source.
        [BsonElement("reviewStatus")]
        public string ReviewStatus { get; set; } = ChatReviewStatus.Unreviewed.ToString();
    }
}