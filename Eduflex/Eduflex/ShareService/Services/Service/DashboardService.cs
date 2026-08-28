using Microsoft.Extensions.Logging;
using ShareService.Enums;
using ShareService.Enums.Roles;
using ShareService.Models.Enquiry;
using ShareService.Models.Enrolment;
using ShareService.Models.Notification;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class DashboardService : IDashboardService
    {
        // Enrolment stages that still need staff action; Cancel/Completed/Finalized are
        // the pipeline's terminal states — nothing left to do on those.
        private static readonly List<EnrolmentEnums> OpenEnrolmentStatuses = new()
        {
            EnrolmentEnums.Draft, EnrolmentEnums.Offer, EnrolmentEnums.Coe,
            EnrolmentEnums.ApplyVisa, EnrolmentEnums.VisaSuccess, EnrolmentEnums.VisaFail,
        };

        // Fixed display order for each module's status breakdown chart — Application and
        // MigrationCase are plain strings (no enum to enumerate), so their order is spelled
        // out here explicitly; Enquiry/Enrolment instead walk their real enum below so a
        // future added enum member shows up automatically.
        private static readonly EnquiryEnums[] EnquiryStatusOrder =
        {
            EnquiryEnums.New, EnquiryEnums.MIR, EnquiryEnums.Responded, EnquiryEnums.Converted,
        };

        private static readonly string[] ApplicationStatusOrder = { "Pending", "Approved", "Rejected", "Studying" };
        private static readonly string[] MigrationCaseStatusOrder = { "Active", "Completed", "Withdrawn" };

        private readonly INotificationService _notificationService;
        private readonly IEnquiryService _enquiryService;
        private readonly IEnrolmentService _enrolmentService;
        private readonly IApplicationService _applicationService;
        private readonly IAccountsService _accountsService;
        private readonly IMigrationCaseService _migrationCaseService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            INotificationService notificationService,
            IEnquiryService enquiryService,
            IEnrolmentService enrolmentService,
            IApplicationService applicationService,
            IAccountsService accountsService,
            IMigrationCaseService migrationCaseService,
            ILogger<DashboardService> logger)
        {
            _notificationService = notificationService;
            _enquiryService = enquiryService;
            _enrolmentService = enrolmentService;
            _applicationService = applicationService;
            _accountsService = accountsService;
            _migrationCaseService = migrationCaseService;
            _logger = logger;
        }

        public async Task<DashboardSummaryModel> GetDashboardSummaryAsync(string userId)
        {
            var notifications = await _notificationService.GetMyNotificationsAsync(userId);

            var counts = new Dictionary<string, int>
            {
                ["Enquiry"] = await CountSafelyAsync("Enquiry", () => CountOpenEnquiriesAsync(userId)),
                ["Application"] = await CountSafelyAsync("Application", () => _applicationService.CountPendingApplicationsAsync(userId)),
                ["Enrolment"] = await CountSafelyAsync("Enrolment", () => CountMyOpenEnrolmentsAsync(userId)),
                ["Finance"] = await CountSafelyAsync("Finance", () => CountFinanceActionQueueAsync(userId)),
            };

            return new DashboardSummaryModel { Notifications = notifications, Counts = counts };
        }

        // One point per calendar month, for the last `months` months (this month
        // included), with all four modules aligned to the same month labels so the
        // frontend can plot them as a multi-series trend chart. Missing permission on a
        // module degrades that module's series to all-zero for this user rather than
        // failing the whole chart, same as GetDashboardSummaryAsync's per-tile
        // CountSafelyAsync below.
        public async Task<MonthlyTrendModel> GetMonthlyTrendsAsync(string userId, int months = 6)
        {
            var firstOfThisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var since = firstOfThisMonth.AddMonths(-(months - 1));

            var enquiryCounts = await CountSafelyDictAsync("Enquiry", () => _enquiryService.GetMonthlyCountsAsync(userId, since));
            var applicationCounts = await CountSafelyDictAsync("Application", () => _applicationService.GetMonthlyCountsAsync(userId, since));
            var enrolmentCounts = await CountSafelyDictAsync("Enrolment", () => _enrolmentService.GetMonthlyCountsAsync(userId, since));
            var migrationCounts = await CountSafelyDictAsync("MigrationCase", () => _migrationCaseService.GetMonthlyCountsAsync(userId, since));

            var points = new List<MonthlyTrendPointModel>();
            for (var i = 0; i < months; i++)
            {
                var monthDate = since.AddMonths(i);
                var key = $"{monthDate.Year:D4}-{monthDate.Month:D2}";
                points.Add(new MonthlyTrendPointModel
                {
                    Month = key,
                    Enquiry = enquiryCounts.GetValueOrDefault(key),
                    Application = applicationCounts.GetValueOrDefault(key),
                    Enrolment = enrolmentCounts.GetValueOrDefault(key),
                    MigrationCase = migrationCounts.GetValueOrDefault(key),
                });
            }

            return new MonthlyTrendModel { Points = points };
        }

        // Current point-in-time pipeline composition per module — "where do things stand
        // today", the complement to GetMonthlyTrendsAsync's "how much came in over time".
        // Every known status is always present (zero-filled), so a module chart's category
        // set never shifts just because nothing currently happens to sit in one status.
        public async Task<StatusBreakdownModel> GetStatusBreakdownAsync(string userId)
        {
            var enquiryCounts = await CountSafelyDictAsync("Enquiry", () => _enquiryService.GetStatusCountsAsync(userId));
            var applicationCounts = await CountSafelyDictAsync("Application", () => _applicationService.GetStatusCountsAsync(userId));
            var enrolmentCounts = await CountSafelyDictAsync("Enrolment", () => _enrolmentService.GetStatusCountsAsync(userId));
            var migrationCounts = await CountSafelyDictAsync("MigrationCase", () => _migrationCaseService.GetStatusCountsAsync(userId));

            return new StatusBreakdownModel
            {
                Enquiry = EnquiryStatusOrder
                    .Select(s => new StatusCountModel { Status = s.ToString(), Label = s.GetDescription(), Count = enquiryCounts.GetValueOrDefault(s.ToString()) })
                    .ToList(),
                Application = BuildStringBreakdown(ApplicationStatusOrder, applicationCounts),
                Enrolment = Enum.GetValues<EnrolmentEnums>()
                    .Select(s => new StatusCountModel { Status = s.ToString(), Label = s.GetDescription(), Count = enrolmentCounts.GetValueOrDefault(s.ToString()) })
                    .ToList(),
                MigrationCase = BuildStringBreakdown(MigrationCaseStatusOrder, migrationCounts),
            };
        }

        private static List<StatusCountModel> BuildStringBreakdown(IReadOnlyList<string> order, Dictionary<string, int> counts)
        {
            return order
                .Select(s => new StatusCountModel { Status = s, Label = s, Count = counts.GetValueOrDefault(s) })
                .ToList();
        }

        private async Task<int> CountOpenEnquiriesAsync(string userId)
        {
            var result = await _enquiryService.GetEnquiries(
                new EnquiryFilter { PageNumber = 1, PageSize = 1, Statuses = new List<EnquiryEnums> { EnquiryEnums.New } },
                userId);
            return result.TotalCount;
        }

        private async Task<int> CountMyOpenEnrolmentsAsync(string userId)
        {
            var result = await _enrolmentService.GetEnrolmentsAsync(
                new EnrolmentFilter { PageNumber = 1, PageSize = 1, Statuses = OpenEnrolmentStatuses, OwnerUserId = userId },
                userId);
            return result.TotalCount;
        }

        private async Task<int> CountFinanceActionQueueAsync(string userId)
        {
            var result = await _accountsService.GetActionQueueAsync(14, userId);
            return result.Items.Count;
        }

        // A staff member without permission on a given module just gets 0 for that tile
        // (matches how the sidebar already hides sections they can't see) — one missing
        // permission shouldn't blank out the whole dashboard summary.
        private async Task<int> CountSafelyAsync(string module, Func<Task<int>> count)
        {
            try
            {
                return await count();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping {Module} count on dashboard summary", module);
                return 0;
            }
        }

        private async Task<Dictionary<string, int>> CountSafelyDictAsync(string module, Func<Task<Dictionary<string, int>>> count)
        {
            try
            {
                return await count();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping {Module} dashboard chart data", module);
                return new Dictionary<string, int>();
            }
        }
    }
}
