namespace Eduflex.DTOs.Accounts
{
    public class AccountSummaryDto
    {
        public string AccountType { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public decimal ContractTotal { get; set; }
        public decimal Received { get; set; }
        public decimal Outstanding { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int OpenCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ActionQueueItemDto
    {
        public string AccountType { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Days { get; set; }
        public decimal Amount { get; set; }
        public string ScheduleLabel { get; set; } = string.Empty;
        public string EntryId { get; set; } = string.Empty;
        public string? LinkedInvoiceId { get; set; }
    }

    public class ActionQueueResultDto
    {
        public List<ActionQueueItemDto> Items { get; set; } = new();
        public int TotalAccounts { get; set; }
        public decimal OverdueAmount { get; set; }
        public int OverdueCount { get; set; }
        public decimal DueToInvoiceAmount { get; set; }
        public int DueToInvoiceCount { get; set; }
    }

    public class AccountTimelineEntryDto
    {
        public string EntryId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string ScheduleStatus { get; set; } = string.Empty;
        public string? SkipReason { get; set; }
        public string? LinkedInvoiceId { get; set; }
        public string? LinkedInvoiceNo { get; set; }
        public string? LinkedInvoiceStatus { get; set; }
        public decimal? LinkedInvoiceTotal { get; set; }
    }

    public class AccountTimelineDto
    {
        public string AccountType { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public decimal ContractTotal { get; set; }
        public decimal Received { get; set; }
        public decimal Outstanding { get; set; }
        public DateTime? NextDueDate { get; set; }
        public List<AccountTimelineEntryDto> Entries { get; set; } = new();
    }
}
