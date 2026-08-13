using Eduflex.DTOs.Accounts;
using ShareService.Models.Accounts;

namespace Eduflex.Mapping.Accounts
{
    public static class AccountsMappingExtension
    {
        public static AccountSummaryDto ToDto(this AccountSummaryModel model)
        {
            return new AccountSummaryDto
            {
                AccountType = model.AccountType,
                AccountKey = model.AccountKey,
                EnrolmentId = model.EnrolmentId,
                Name = model.Name,
                SubLabel = model.SubLabel,
                ContractTotal = model.ContractTotal,
                Received = model.Received,
                Outstanding = model.Outstanding,
                NextDueDate = model.NextDueDate,
                OpenCount = model.OpenCount,
                Status = model.Status
            };
        }

        public static ActionQueueItemDto ToDto(this ActionQueueItemModel model)
        {
            return new ActionQueueItemDto
            {
                AccountType = model.AccountType,
                AccountKey = model.AccountKey,
                EnrolmentId = model.EnrolmentId,
                Name = model.Name,
                SubLabel = model.SubLabel,
                Reason = model.Reason,
                Days = model.Days,
                Amount = model.Amount,
                ScheduleLabel = model.ScheduleLabel,
                EntryId = model.EntryId,
                LinkedInvoiceId = model.LinkedInvoiceId
            };
        }

        public static ActionQueueResultDto ToDto(this ActionQueueResultModel model)
        {
            return new ActionQueueResultDto
            {
                Items = model.Items.Select(i => i.ToDto()).ToList(),
                TotalAccounts = model.TotalAccounts,
                OverdueAmount = model.OverdueAmount,
                OverdueCount = model.OverdueCount,
                DueToInvoiceAmount = model.DueToInvoiceAmount,
                DueToInvoiceCount = model.DueToInvoiceCount
            };
        }

        public static AccountTimelineEntryDto ToDto(this AccountTimelineEntryModel model)
        {
            return new AccountTimelineEntryDto
            {
                EntryId = model.EntryId,
                FeeType = model.FeeType,
                Label = model.Label,
                DueDate = model.DueDate,
                Amount = model.Amount,
                ScheduleStatus = model.ScheduleStatus,
                SkipReason = model.SkipReason,
                LinkedInvoiceId = model.LinkedInvoiceId,
                LinkedInvoiceNo = model.LinkedInvoiceNo,
                LinkedInvoiceStatus = model.LinkedInvoiceStatus,
                LinkedInvoiceTotal = model.LinkedInvoiceTotal
            };
        }

        public static AccountTimelineDto ToDto(this AccountTimelineModel model)
        {
            return new AccountTimelineDto
            {
                AccountType = model.AccountType,
                AccountKey = model.AccountKey,
                EnrolmentId = model.EnrolmentId,
                Name = model.Name,
                SubLabel = model.SubLabel,
                ContractTotal = model.ContractTotal,
                Received = model.Received,
                Outstanding = model.Outstanding,
                NextDueDate = model.NextDueDate,
                Entries = model.Entries.Select(e => e.ToDto()).ToList()
            };
        }
    }
}
