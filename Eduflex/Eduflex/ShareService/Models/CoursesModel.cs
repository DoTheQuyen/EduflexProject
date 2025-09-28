using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.Shared.Models;

public class Courses
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
}