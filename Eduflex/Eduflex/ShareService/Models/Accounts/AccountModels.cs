namespace ShareService.Models.Accounts
{
    // Not persisted — these are computed view models the AccountsService assembles on
    // read by joining StudentPaymentPlanEntries / FinancialRecord.InvoicePlan against
    // the Invoice ledger. "Account" granularity: one Student account = one Enrolment;
    // one Partner account = one FinancialRecord (i.e. one partner-per-enrolment
    // commission relationship, not an aggregate across every enrolment a partner has —
    // a partner with five enrolments shows five accounts, not one rolled-up total).
    public static class AccountTypes
    {
        public const string Student = "Student";
        public const string BusinessPartner = "BusinessPartner";
        public const string EducationPartner = "EducationPartner";
    }

    public static class AccountStatuses
    {
        public const string OnTrack = "OnTrack";
        public const string AtRisk = "AtRisk";
        public const string Overdue = "Overdue";
        public const string Complete = "Complete";
    }

    // Why one Action Queue row exists: NotInvoiced = a Planned entry due inside the
    // window that hasn't been sent yet; Overdue = past its due date and still unpaid
    // (whether or not it's been invoiced); Failed = an invoice went out for this entry
    // but the send failed and needs a resend.
    public static class ActionQueueReasons
    {
        public const string NotInvoiced = "NotInvoiced";
        public const string Overdue = "Overdue";
        public const string Failed = "Failed";
    }

    public class AccountSummaryModel
    {
        public string AccountType { get; set; } = string.Empty;
        // EnrolmentId for a Student account, FinancialRecordId for a Partner account —
        // what Account Timeline takes as its lookup key.
        public string AccountKey { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public decimal ContractTotal { get; set; }
        public decimal Received { get; set; }
        public decimal Outstanding { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int OpenCount { get; set; }
        public string Status { get; set; } = AccountStatuses.OnTrack;
    }

    public class ActionQueueItemModel
    {
        public string AccountType { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string EnrolmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public string Reason { get; set; } = string.Empty;
        // Positive day count either way — Reason says whether it means "days overdue" or
        // "days until due".
        public int Days { get; set; }
        public decimal Amount { get; set; }
        public string ScheduleLabel { get; set; } = string.Empty;
        public string EntryId { get; set; } = string.Empty;
        public string? LinkedInvoiceId { get; set; }
    }

    public class ActionQueueResultModel
    {
        public List<ActionQueueItemModel> Items { get; set; } = new();
        public int TotalAccounts { get; set; }
        public decimal OverdueAmount { get; set; }
        public int OverdueCount { get; set; }
        public decimal DueToInvoiceAmount { get; set; }
        public int DueToInvoiceCount { get; set; }
    }

    public class AccountTimelineEntryModel
    {
        public string EntryId { get; set; } = string.Empty;
        // One of StudentFeeTypes for a Student account entry (Tuition/ServiceFee/
        // VisaExtension/Visa485/PartnerVisa/Other), or the literal "Commission" for a
        // Partner account entry — there's only one fee type on that side today.
        public string FeeType { get; set; } = string.Empty;
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

    public class AccountTimelineModel
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
        public List<AccountTimelineEntryModel> Entries { get; set; } = new();
    }
}
