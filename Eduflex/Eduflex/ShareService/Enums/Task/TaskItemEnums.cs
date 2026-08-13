using System.Text.Json.Serialization;

namespace ShareService.Enums.Task
{
    // Named TaskItem*, not Task*, everywhere in this module — "Task" collides with
    // System.Threading.Tasks.Task, which every async method in this codebase returns.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskItemStatus
    {
        New = 1,
        Processing = 2,
        Completed = 3,
    }

    // Distinguishes a manually-typed note from the system-generated timeline entries a
    // status change or reassignment appends, so the frontend can render them differently
    // (e.g. italic/system-styled vs a normal note bubble) without parsing Content text.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskNoteType
    {
        Note = 1,
        StatusChange = 2,
        Reassign = 3,
    }
}
