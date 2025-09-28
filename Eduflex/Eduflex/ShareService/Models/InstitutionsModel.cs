using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.Shared.Models;

public class Institutions
{
    [BsonElement("_t")]
    public int T { get; set; }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("StateMachine")]
    public int StateMachine { get; set; }
}