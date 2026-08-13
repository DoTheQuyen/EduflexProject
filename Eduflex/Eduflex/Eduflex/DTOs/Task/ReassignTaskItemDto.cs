namespace Eduflex.DTOs.Task
{
    public class ReassignTaskItemDto
    {
        public string NewAssigneeUserId { get; set; } = string.Empty;

        // Required — every reassignment records why, as its own timeline entry
        // (see TaskNoteType.Reassign).
        public string Note { get; set; } = string.Empty;
    }
}
