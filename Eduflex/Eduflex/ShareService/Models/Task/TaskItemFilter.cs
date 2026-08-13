using ShareService.Common;
using ShareService.Enums.Task;

namespace ShareService.Models.Task
{
    public class TaskItemFilter : PaginationQuery
    {
        public string? SearchTerm { get; set; }
        public TaskItemStatus? Status { get; set; }

        // Set by the task-list component's "Active" tab (New + Processing together) —
        // Status alone can only match one status, and there's no clean single-value way
        // to say "either of these two", so the Active tab excludes Completed instead of
        // trying to include two statuses. Mutually exclusive with Status in practice
        // (the Completed tab sets Status=Completed and leaves this null).
        public TaskItemStatus? ExcludeStatus { get; set; }

        // At most one of these is normally set at a time — whichever detail page's
        // Tasks tab issued the search. Left null for My Tasks / All Tasks searches.
        public string? EnrolmentId { get; set; }
        public string? EnquiryId { get; set; }
        public string? ApplicationId { get; set; }
        public string? FinancialRecordId { get; set; }
        public string? MigrationCaseId { get; set; }
    }
}
