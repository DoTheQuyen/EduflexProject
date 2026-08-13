using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Task;
using ShareService.Models.Common;

namespace ShareService.Models.Task
{
    [BsonIgnoreExtraElements]
    public class TaskItemModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        [BsonRequired]
        public string Name { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        // The creator. Fixed at creation time — reassignment only ever moves
        // AssigneeUserId, never who originally raised the task.
        [BsonElement("assignerUserId")]
        public string AssignerUserId { get; set; } = string.Empty;

        // The current owner of the work. Changes on reassignment.
        [BsonElement("assigneeUserId")]
        public string AssigneeUserId { get; set; } = string.Empty;

        [BsonElement("dueDateTime")]
        public DateTime DueDateTime { get; set; }

        // Stored as string (TaskItemStatus.ToString()), same convention as
        // EnrolmentModel.Status — keeps Mongo documents human-readable and avoids
        // renumbering pain if the enum's members are ever reordered.
        [BsonElement("status")]
        public string Status { get; set; } = TaskItemStatus.New.ToString();

        // Optional links to the record this task is about. Plain nullable ObjectId
        // strings, no cached display fields — same convention as EnrolmentModel's
        // EnquiryId/StudentApplicationId links. The frontend resolves a display label
        // via the linked module's own API when it needs one.
        [BsonElement("enrolmentId")]
        public string? EnrolmentId { get; set; }

        [BsonElement("enquiryId")]
        public string? EnquiryId { get; set; }

        [BsonElement("applicationId")]
        public string? ApplicationId { get; set; }

        [BsonElement("financialRecordId")]
        public string? FinancialRecordId { get; set; }

        [BsonElement("migrationCaseId")]
        public string? MigrationCaseId { get; set; }

        // Append-only timeline: manual notes plus system-generated entries for every
        // status change and reassignment. Never edited/removed, only appended to.
        [BsonElement("notes")]
        public List<TaskNoteModel> Notes { get; set; } = new();
    }
}
