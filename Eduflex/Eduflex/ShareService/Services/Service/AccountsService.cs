using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Accounts;
using ShareService.Models.Invoice;
using ShareService.Models.StudentPaymentPlan;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class AccountsService : IAccountsService
    {
        private readonly IStudentPaymentPlanEntry _studentPlanDataAccess;
        private readonly IFinancialRecord _financialRecordDataAccess;
        private readonly IInvoice _invoiceDataAccess;
        private readonly IEnrolment _enrolmentDataAccess;
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly IBusinessPartner _businessPartnerDataAccess;
        private readonly IPermissionService _permissionService;

        public AccountsService(
            IStudentPaymentPlanEntry studentPlanDataAccess,
            IFinancialRecord financialRecordDataAccess,
            IInvoice invoiceDataAccess,
            IEnrolment enrolmentDataAccess,
            IEducationPartner educationPartnerDataAccess,
            IBusinessPartner businessPartnerDataAccess,
            IPermissionService permissionService)
        {
            _studentPlanDataAccess = studentPlanDataAccess;
            _financialRecordDataAccess = financialRecordDataAccess;
            _invoiceDataAccess = invoiceDataAccess;
            _enrolmentDataAccess = enrolmentDataAccess;
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _businessPartnerDataAccess = businessPartnerDataAccess;
            _permissionService = permissionService;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // ===================== Action Queue =====================

        public async Task<ActionQueueResultModel> GetActionQueueAsync(int windowDays, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view the action queue");

            var today = DateTime.UtcNow.Date;
            var cutoff = today.AddDays(windowDays);
            var items = new List<ActionQueueItemModel>();

            // --- Student instalments ---
            var dueStudentEntries = await _studentPlanDataAccess.GetDueByAsync(cutoff);
            foreach (var entry in dueStudentEntries)
            {
                var item = await BuildStudentQueueItemAsync(entry, today);
                if (item != null) items.Add(item);
            }

            // --- Partner commission claims ---
            var allRecords = await _financialRecordDataAccess.GetAllAsync();
            var context = await BuildPartnerContextAsync(allRecords);

            foreach (var record in allRecords)
            {
                for (var i = 0; i < record.InvoicePlan.Count; i++)
                {
                    var entry = record.InvoicePlan[i];
                    if (entry.Status == "Skipped" || entry.ClaimDate.Date > cutoff) continue;

                    var item = await BuildPartnerQueueItemAsync(record, entry, i, today, context);
                    if (item != null) items.Add(item);
                }
            }

            // --- Portfolio size, for "N of M accounts" framing ---
            var allStudentEntries = await _studentPlanDataAccess.GetAllAsync();
            var totalAccounts = allStudentEntries.Select(e => e.EnrolmentId).Distinct().Count() + allRecords.Count;

            var ordered = items
                .OrderBy(i => i.Reason == ActionQueueReasons.Overdue ? 0 : i.Reason == ActionQueueReasons.Failed ? 1 : 2)
                .ThenByDescending(i => i.Reason == ActionQueueReasons.Overdue ? i.Days : -i.Days)
                .ToList();

            return new ActionQueueResultModel
            {
                Items = ordered,
                TotalAccounts = totalAccounts,
                OverdueAmount = items.Where(i => i.Reason == ActionQueueReasons.Overdue).Sum(i => i.Amount),
                OverdueCount = items.Count(i => i.Reason == ActionQueueReasons.Overdue),
                DueToInvoiceAmount = items.Where(i => i.Reason == ActionQueueReasons.NotInvoiced).Sum(i => i.Amount),
                DueToInvoiceCount = items.Count(i => i.Reason == ActionQueueReasons.NotInvoiced)
            };
        }

        private async Task<ActionQueueItemModel?> BuildStudentQueueItemAsync(StudentPaymentPlanEntryModel entry, DateTime today)
        {
            var (reason, days, amount) = entry.Status switch
            {
                "Planned" when entry.DueDate.Date < today => (ActionQueueReasons.Overdue, (today - entry.DueDate.Date).Days, entry.Amount),
                "Planned" => (ActionQueueReasons.NotInvoiced, (entry.DueDate.Date - today).Days, entry.Amount),
                _ => (string.Empty, 0, 0m)
            };

            if (reason == string.Empty && entry.Status == "Invoiced" && !string.IsNullOrEmpty(entry.LinkedInvoiceId))
            {
                var invoice = await _invoiceDataAccess.GetByIdAsync(entry.LinkedInvoiceId);
                if (invoice == null || invoice.Status == InvoiceStatuses.Paid) return null;

                if (invoice.Status == InvoiceStatuses.Failed)
                {
                    (reason, days, amount) = (ActionQueueReasons.Failed, 0, invoice.Total);
                }
                else if (invoice.Status == InvoiceStatuses.Sent && entry.DueDate.Date < today)
                {
                    (reason, days, amount) = (ActionQueueReasons.Overdue, (today - entry.DueDate.Date).Days, invoice.Total);
                }
                else
                {
                    return null; // Sent, not yet due — on track, not actionable yet.
                }
            }
            else if (reason == string.Empty)
            {
                return null; // Skipped, or Invoiced with no linked invoice on record.
            }

            return new ActionQueueItemModel
            {
                AccountType = AccountTypes.Student,
                AccountKey = entry.EnrolmentId,
                EnrolmentId = entry.EnrolmentId,
                Name = entry.StudentName,
                SubLabel = entry.CourseName,
                Reason = reason,
                Days = days,
                Amount = amount,
                ScheduleLabel = entry.Label,
                EntryId = entry.Id,
                LinkedInvoiceId = entry.LinkedInvoiceId
            };
        }

        private async Task<ActionQueueItemModel?> BuildPartnerQueueItemAsync(
            ShareService.Models.Financial.FinancialRecordModel record, ShareService.Models.Financial.InvoicePlanEntryModel entry,
            int index, DateTime today, PartnerContext context)
        {
            var (reason, days, amount) = entry.Status switch
            {
                "Planned" when entry.ClaimDate.Date < today => (ActionQueueReasons.Overdue, (today - entry.ClaimDate.Date).Days, 0m),
                "Planned" => (ActionQueueReasons.NotInvoiced, (entry.ClaimDate.Date - today).Days, 0m),
                _ => (string.Empty, 0, 0m)
            };

            if (reason == string.Empty && entry.Status == "Invoiced" && !string.IsNullOrEmpty(entry.LinkedInvoiceId))
            {
                var invoice = await _invoiceDataAccess.GetByIdAsync(entry.LinkedInvoiceId);
                if (invoice == null || invoice.Status == InvoiceStatuses.Paid) return null;

                if (invoice.Status == InvoiceStatuses.Failed)
                {
                    (reason, days, amount) = (ActionQueueReasons.Failed, 0, invoice.Total);
                }
                else if (invoice.Status == InvoiceStatuses.Sent && entry.ClaimDate.Date < today)
                {
                    (reason, days, amount) = (ActionQueueReasons.Overdue, (today - entry.ClaimDate.Date).Days, invoice.Total);
                }
                else
                {
                    return null;
                }
            }
            else if (reason == string.Empty)
            {
                return null;
            }

            var (accountType, name) = context.ResolvePartner(record);
            return new ActionQueueItemModel
            {
                AccountType = accountType,
                AccountKey = record.Id,
                EnrolmentId = record.EnrolmentId,
                Name = name,
                SubLabel = context.ResolveStudentName(record.EnrolmentId),
                Reason = reason,
                Days = days,
                Amount = amount,
                ScheduleLabel = $"Claim {index + 1} of {record.InvoicePlan.Count}",
                EntryId = entry.Id,
                LinkedInvoiceId = entry.LinkedInvoiceId
            };
        }

        // ===================== Accounts portfolio =====================

        public async Task<PagedResult<AccountSummaryModel>> GetPortfolioAsync(
            string? search, string? accountType, string? status, int pageNumber, int pageSize, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view accounts");

            var summaries = new List<AccountSummaryModel>();

            if (string.IsNullOrEmpty(accountType) || accountType == AccountTypes.Student)
            {
                var allStudentEntries = await _studentPlanDataAccess.GetAllAsync();
                foreach (var group in allStudentEntries.GroupBy(e => e.EnrolmentId))
                {
                    var invoices = await _invoiceDataAccess.GetByEnrolmentIdAsync(group.Key);
                    summaries.Add(BuildStudentSummary(group.Key, group.ToList(), invoices));
                }
            }

            if (string.IsNullOrEmpty(accountType) || accountType != AccountTypes.Student)
            {
                var allRecords = await _financialRecordDataAccess.GetAllAsync();
                var context = await BuildPartnerContextAsync(allRecords);

                foreach (var record in allRecords)
                {
                    var (recordAccountType, _) = context.ResolvePartner(record);
                    if (!string.IsNullOrEmpty(accountType) && accountType != recordAccountType) continue;

                    var invoices = await _invoiceDataAccess.GetByFinancialRecordIdAsync(record.Id);
                    summaries.Add(BuildPartnerSummary(record, invoices, context));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                summaries = summaries.Where(s =>
                    s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (s.SubLabel?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                summaries = summaries.Where(s => s.Status == status).ToList();
            }

            summaries = summaries
                .OrderBy(s => s.Status == AccountStatuses.Overdue ? 0 : s.Status == AccountStatuses.AtRisk ? 1 : s.Status == AccountStatuses.OnTrack ? 2 : 3)
                .ThenBy(s => s.Name)
                .ToList();

            var totalCount = summaries.Count;
            var page = summaries.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<AccountSummaryModel>
            {
                Items = page,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static AccountSummaryModel BuildStudentSummary(
            string enrolmentId, List<StudentPaymentPlanEntryModel> entries, List<InvoiceModel> invoices)
        {
            var invoiceById = invoices.ToDictionary(i => i.Id);
            var contractTotal = entries.Sum(e => e.Amount);
            var received = entries
                .Where(e => e.LinkedInvoiceId != null && invoiceById.TryGetValue(e.LinkedInvoiceId, out var inv) && inv.Status == InvoiceStatuses.Paid)
                .Sum(e => e.Amount);
            var openEntries = entries.Where(e => e.Status != "Skipped" &&
                !(e.LinkedInvoiceId != null && invoiceById.TryGetValue(e.LinkedInvoiceId, out var inv2) && inv2.Status == InvoiceStatuses.Paid)).ToList();
            var nextDue = openEntries.Where(e => e.Status == "Planned").OrderBy(e => e.DueDate).Select(e => (DateTime?)e.DueDate).FirstOrDefault();

            var first = entries.First();
            return new AccountSummaryModel
            {
                AccountType = AccountTypes.Student,
                AccountKey = enrolmentId,
                EnrolmentId = enrolmentId,
                Name = first.StudentName,
                SubLabel = first.CourseName,
                ContractTotal = contractTotal,
                Received = received,
                Outstanding = Math.Max(0, contractTotal - received),
                NextDueDate = nextDue,
                OpenCount = openEntries.Count,
                Status = DeriveStatus(contractTotal, received, openEntries.Count, nextDue)
            };
        }

        private static AccountSummaryModel BuildPartnerSummary(ShareService.Models.Financial.FinancialRecordModel record, List<InvoiceModel> invoices, PartnerContext context)
        {
            var invoiceById = invoices.ToDictionary(i => i.Id);
            var contractTotal = record.ExpectedCommission + record.ExtraCommissionAdjustments.Sum(a => a.Amount);
            var received = invoices.Where(i => i.Status == InvoiceStatuses.Paid).Sum(i => i.Total);
            var openEntries = record.InvoicePlan.Where(e => e.Status != "Skipped" &&
                !(e.LinkedInvoiceId != null && invoiceById.TryGetValue(e.LinkedInvoiceId, out var inv) && inv.Status == InvoiceStatuses.Paid)).ToList();
            var nextDue = openEntries.Where(e => e.Status == "Planned").OrderBy(e => e.ClaimDate).Select(e => (DateTime?)e.ClaimDate).FirstOrDefault();

            var (accountType, name) = context.ResolvePartner(record);
            return new AccountSummaryModel
            {
                AccountType = accountType,
                AccountKey = record.Id,
                EnrolmentId = record.EnrolmentId,
                Name = name,
                SubLabel = context.ResolveStudentName(record.EnrolmentId),
                ContractTotal = contractTotal,
                Received = received,
                Outstanding = Math.Max(0, contractTotal - received),
                NextDueDate = nextDue,
                OpenCount = openEntries.Count,
                Status = DeriveStatus(contractTotal, received, openEntries.Count, nextDue)
            };
        }

        private static string DeriveStatus(decimal contractTotal, decimal received, int openCount, DateTime? nextDue)
        {
            var today = DateTime.UtcNow.Date;
            if (openCount == 0 && received >= contractTotal) return AccountStatuses.Complete;
            if (nextDue.HasValue && nextDue.Value.Date < today) return AccountStatuses.Overdue;
            if (nextDue.HasValue && (nextDue.Value.Date - today).Days <= 14) return AccountStatuses.AtRisk;
            return AccountStatuses.OnTrack;
        }

        // ===================== Account timeline =====================

        public async Task<AccountTimelineModel> GetAccountTimelineAsync(string accountType, string accountKey, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view account timelines");

            if (accountType == AccountTypes.Student)
            {
                var entries = await _studentPlanDataAccess.GetByEnrolmentIdAsync(accountKey);
                if (entries.Count == 0) throw new KeyNotFoundException("No payment plan found for this account");

                var invoices = await _invoiceDataAccess.GetByEnrolmentIdAsync(accountKey);
                var invoiceById = invoices.ToDictionary(i => i.Id);
                var first = entries.First();

                var timelineEntries = entries.Select(e => BuildTimelineEntry(
                    e.Id, e.FeeType, e.Label, e.DueDate, e.Amount, e.Status, e.SkipReason, e.LinkedInvoiceId, invoiceById)).ToList();

                var contractTotal = entries.Sum(e => e.Amount);
                var received = timelineEntries.Where(e => e.LinkedInvoiceStatus == InvoiceStatuses.Paid).Sum(e => e.Amount);
                var nextDue = entries.Where(e => e.Status == "Planned").OrderBy(e => e.DueDate).Select(e => (DateTime?)e.DueDate).FirstOrDefault();

                return new AccountTimelineModel
                {
                    AccountType = AccountTypes.Student,
                    AccountKey = accountKey,
                    EnrolmentId = accountKey,
                    Name = first.StudentName,
                    SubLabel = first.CourseName,
                    ContractTotal = contractTotal,
                    Received = received,
                    Outstanding = Math.Max(0, contractTotal - received),
                    NextDueDate = nextDue,
                    Entries = timelineEntries
                };
            }
            else
            {
                var record = await _financialRecordDataAccess.GetByIdAsync(accountKey)
                    ?? throw new KeyNotFoundException("Financial record not found");

                var invoices = await _invoiceDataAccess.GetByFinancialRecordIdAsync(accountKey);
                var invoiceById = invoices.ToDictionary(i => i.Id);
                var context = await BuildPartnerContextAsync(new List<ShareService.Models.Financial.FinancialRecordModel> { record });

                var timelineEntries = record.InvoicePlan.Select((e, i) => BuildTimelineEntry(
                    e.Id, "Commission", $"Claim {i + 1} of {record.InvoicePlan.Count}", e.ClaimDate, 0m, e.Status, e.SkipReason, e.LinkedInvoiceId, invoiceById)).ToList();

                var contractTotal = record.ExpectedCommission + record.ExtraCommissionAdjustments.Sum(a => a.Amount);
                var received = invoices.Where(i => i.Status == InvoiceStatuses.Paid).Sum(i => i.Total);
                var nextDue = record.InvoicePlan.Where(e => e.Status == "Planned").OrderBy(e => e.ClaimDate).Select(e => (DateTime?)e.ClaimDate).FirstOrDefault();

                var (resolvedType, name) = context.ResolvePartner(record);
                return new AccountTimelineModel
                {
                    AccountType = resolvedType,
                    AccountKey = accountKey,
                    EnrolmentId = record.EnrolmentId,
                    Name = name,
                    SubLabel = context.ResolveStudentName(record.EnrolmentId),
                    ContractTotal = contractTotal,
                    Received = received,
                    Outstanding = Math.Max(0, contractTotal - received),
                    NextDueDate = nextDue,
                    Entries = timelineEntries
                };
            }
        }

        private static AccountTimelineEntryModel BuildTimelineEntry(
            string entryId, string feeType, string label, DateTime dueDate, decimal fallbackAmount, string status, string? skipReason,
            string? linkedInvoiceId, Dictionary<string, InvoiceModel> invoiceById)
        {
            InvoiceModel? invoice = null;
            if (linkedInvoiceId != null) invoiceById.TryGetValue(linkedInvoiceId, out invoice);

            return new AccountTimelineEntryModel
            {
                EntryId = entryId,
                FeeType = feeType,
                Label = label,
                DueDate = dueDate,
                Amount = invoice?.Total ?? fallbackAmount,
                ScheduleStatus = status,
                SkipReason = skipReason,
                LinkedInvoiceId = linkedInvoiceId,
                LinkedInvoiceNo = invoice?.InvoiceNo,
                LinkedInvoiceStatus = invoice?.Status,
                LinkedInvoiceTotal = invoice?.Total
            };
        }

        // ===================== Shared partner name/context resolution =====================

        private async Task<PartnerContext> BuildPartnerContextAsync(List<ShareService.Models.Financial.FinancialRecordModel> records)
        {
            var enrolmentIds = records.Select(r => r.EnrolmentId).Distinct();
            var enrolments = await _enrolmentDataAccess.GetByIdsAsync(enrolmentIds);

            var eduIds = records.Where(r => !string.IsNullOrEmpty(r.EducationPartnerId)).Select(r => r.EducationPartnerId!).Distinct();
            var eduPartners = await _educationPartnerDataAccess.GetByIdsAsync(eduIds);

            var bizIds = records.Where(r => !string.IsNullOrEmpty(r.BusinessPartnerId)).Select(r => r.BusinessPartnerId!).Distinct();
            var bizPartners = await _businessPartnerDataAccess.GetByIdsAsync(bizIds);

            return new PartnerContext(
                enrolments.ToDictionary(e => e.Id),
                eduPartners.ToDictionary(p => p.Id),
                bizPartners.ToDictionary(p => p.Id));
        }

        private class PartnerContext
        {
            private readonly Dictionary<string, ShareService.Models.Enrolment.EnrolmentModel> _enrolmentById;
            private readonly Dictionary<string, ShareService.Models.EducationPartner.EducationPartnerModel> _eduPartnerById;
            private readonly Dictionary<string, ShareService.Models.BusinessPartner.BusinessPartnerModel> _bizPartnerById;

            public PartnerContext(
                Dictionary<string, ShareService.Models.Enrolment.EnrolmentModel> enrolmentById,
                Dictionary<string, ShareService.Models.EducationPartner.EducationPartnerModel> eduPartnerById,
                Dictionary<string, ShareService.Models.BusinessPartner.BusinessPartnerModel> bizPartnerById)
            {
                _enrolmentById = enrolmentById;
                _eduPartnerById = eduPartnerById;
                _bizPartnerById = bizPartnerById;
            }

            // A Business Partner (agent) relationship takes priority over a direct
            // Education Partner one — mirrors FinancialRecordModel's own commission-rate
            // stacking rule (see its ExpectedCommission comment): a Business Partner link
            // means the commission flows through the agent, not straight to the college.
            public (string AccountType, string Name) ResolvePartner(ShareService.Models.Financial.FinancialRecordModel record)
            {
                if (!string.IsNullOrEmpty(record.BusinessPartnerId) && _bizPartnerById.TryGetValue(record.BusinessPartnerId, out var biz))
                {
                    return (AccountTypes.BusinessPartner, biz.Name);
                }
                if (!string.IsNullOrEmpty(record.EducationPartnerId) && _eduPartnerById.TryGetValue(record.EducationPartnerId, out var edu))
                {
                    return (AccountTypes.EducationPartner, edu.Name);
                }
                return (AccountTypes.EducationPartner, "Unknown partner");
            }

            public string? ResolveStudentName(string enrolmentId)
            {
                if (!_enrolmentById.TryGetValue(enrolmentId, out var enrolment)) return null;
                return $"{enrolment.FirstName} {enrolment.LastName}".Trim();
            }
        }
    }
}
