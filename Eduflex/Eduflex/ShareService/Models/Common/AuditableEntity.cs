using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ShareService.Models.Common
{
    public abstract class AuditableEntity : IAuditableEntity
    {
        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedBy")]
        public string? UpdatedBy { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}