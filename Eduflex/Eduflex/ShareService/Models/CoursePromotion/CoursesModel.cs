using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.CoursePromotion;

public class CoursesModel
{
    [BsonElement("_t")]
    public int T { get; set; }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("StateMachine")]
    public int StateMachine { get; set; }
}