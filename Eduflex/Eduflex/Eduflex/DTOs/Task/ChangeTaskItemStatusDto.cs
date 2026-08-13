namespace Eduflex.DTOs.Task
{
    public class ChangeTaskItemStatusDto
    {
        // "Processing" | "Completed" — see TaskItemService.ChangeStatusAsync for the
        // allowed-transition whitelist. "New" is never a valid target here; it's only
        // ever the value a task is created with.
        public string Status { get; set; } = string.Empty;
    }
}
