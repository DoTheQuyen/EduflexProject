using Microsoft.Extensions.Logging;
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

        private readonly INotificationService _notificationService;
        private readonly IEnquiryService _enquiryService;
        private readonly IEnrolmentService _enrolmentService;
        private readonly IApplicationService _applicationService;
        private readonly IAccountsService _accountsService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            INotificationService notificationService,
            IEnquiryService enquiryService,
            IEnrolmentService enrolmentService,
            IApplicationService applicationService,
            IAccountsService accountsService,
            ILogger<DashboardService> logger)
        {
            _notificationService = notificationService;
            _enquiryService = enquiryService;
            _enrolmentService = enrolmentService;
            _applicationService = applicationService;
            _accountsService = accountsService;
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
    }
}
