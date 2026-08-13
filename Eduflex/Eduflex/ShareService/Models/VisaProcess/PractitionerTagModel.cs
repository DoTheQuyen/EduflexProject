using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.VisaProcess
{
    // A small, business-managed catalog of staffing/routing labels — see
    // docs/09-visa-process-config-module-design.md §C.9. Assigned to a
    // VisaProcessStepDefinitionModel via PractitionerTagId; purely informational, no access
    // control or enforcement anywhere. No fixed vocabulary — a business names whatever
    // labels suit its own structure.
    public class PractitionerTagModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("colorHex")]
        public string ColorHex { get; set; } = "#5c6b7a";

        [BsonElement("description")]
        public string? Description { get; set; }

        // Deactivating (not deleting) stops a tag appearing as a selectable option for new
        // assignments — steps already using it keep their PractitionerTagId untouched, same
        // non-destructive pattern as VisaProcessTemplateModel.Status.
        [BsonElement("active")]
        public bool Active { get; set; } = true;
    }
}
