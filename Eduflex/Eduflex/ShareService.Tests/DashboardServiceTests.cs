using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.Common;
using ShareService.Models.Accounts;
using ShareService.Models.Enquiry;
using ShareService.Models.Enrolment;
using ShareService.Models.Notification;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class DashboardServiceTests
    {
        private const string UserId = "user-1";

        private Mock<INotificationService> _notificationServiceMock;
        private Mock<IEnquiryService> _enquiryServiceMock;
        private Mock<IEnrolmentService> _enrolmentServiceMock;
        private Mock<IApplicationService> _applicationServiceMock;
        private Mock<IAccountsService> _accountsServiceMock;
        private Mock<IMigrationCaseService> _migrationCaseServiceMock;
        private Mock<ILogger<DashboardService>> _loggerMock;
        private DashboardService _service;

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _enquiryServiceMock = new Mock<IEnquiryService>();
            _enrolmentServiceMock = new Mock<IEnrolmentService>();
            _applicationServiceMock = new Mock<IApplicationService>();
            _accountsServiceMock = new Mock<IAccountsService>();
            _migrationCaseServiceMock = new Mock<IMigrationCaseService>();
            _loggerMock = new Mock<ILogger<DashboardService>>();

            _notificationServiceMock
                .Setup(n => n.GetMyNotificationsAsync(UserId))
                .ReturnsAsync(new List<NotificationModel>());

            _enquiryServiceMock
                .Setup(e => e.GetEnquiries(It.IsAny<EnquiryFilter>(), UserId))
                .ReturnsAsync(new PagedResult<EnquiryModel> { TotalCount = 3 });

            _applicationServiceMock
                .Setup(a => a.CountPendingApplicationsAsync(UserId))
                .ReturnsAsync(5);

            _enrolmentServiceMock
                .Setup(e => e.GetEnrolmentsAsync(It.IsAny<EnrolmentFilter>(), UserId))
                .ReturnsAsync(new PagedResult<EnrolmentModel> { TotalCount = 2 });

            _accountsServiceMock
                .Setup(a => a.GetActionQueueAsync(14, UserId))
                .ReturnsAsync(new ActionQueueResultModel { Items = new List<ActionQueueItemModel> { new() } });

            _service = new DashboardService(
                _notificationServiceMock.Object,
                _enquiryServiceMock.Object,
                _enrolmentServiceMock.Object,
                _applicationServiceMock.Object,
                _accountsServiceMock.Object,
                _migrationCaseServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetDashboardSummaryAsync_AggregatesCountsFromEachModule()
        {
            var result = await _service.GetDashboardSummaryAsync(UserId);

            Assert.That(result.Counts["Enquiry"], Is.EqualTo(3));
            Assert.That(result.Counts["Application"], Is.EqualTo(5));
            Assert.That(result.Counts["Enrolment"], Is.EqualTo(2));
            Assert.That(result.Counts["Finance"], Is.EqualTo(1));
        }

        [Test]
        public async Task GetDashboardSummaryAsync_IncludesNotifications()
        {
            _notificationServiceMock
                .Setup(n => n.GetMyNotificationsAsync(UserId))
                .ReturnsAsync(new List<NotificationModel> { new() { Id = "1", Summary = "Hello" } });

            var result = await _service.GetDashboardSummaryAsync(UserId);

            Assert.That(result.Notifications, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetDashboardSummaryAsync_DefaultsModuleCountToZero_WhenModuleThrows()
        {
            _applicationServiceMock
                .Setup(a => a.CountPendingApplicationsAsync(UserId))
                .ThrowsAsync(new UnauthorizedAccessException("no permission"));

            var result = await _service.GetDashboardSummaryAsync(UserId);

            Assert.That(result.Counts["Application"], Is.EqualTo(0));
            Assert.That(result.Counts["Enquiry"], Is.EqualTo(3));
        }

        [Test]
        public async Task GetMonthlyTrendsAsync_BuildsOnePointPerMonth()
        {
            _enquiryServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());
            _applicationServiceMock
                .Setup(a => a.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());
            _enrolmentServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());

            var result = await _service.GetMonthlyTrendsAsync(UserId, 3);

            Assert.That(result.Points, Has.Count.EqualTo(3));
        }

        [Test]
        public async Task GetMonthlyTrendsAsync_MapsCountsToCorrectMonthKey()
        {
            var thisMonthKey = $"{DateTime.UtcNow:yyyy-MM}";

            _enquiryServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int> { { thisMonthKey, 7 } });
            _applicationServiceMock
                .Setup(a => a.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());
            _enrolmentServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());

            var result = await _service.GetMonthlyTrendsAsync(UserId, 1);

            Assert.That(result.Points[0].Enquiry, Is.EqualTo(7));
        }

        [Test]
        public async Task GetMonthlyTrendsAsync_DefaultsSeriesToZero_WhenModuleThrows()
        {
            _enrolmentServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ThrowsAsync(new UnauthorizedAccessException("no permission"));
            _enquiryServiceMock
                .Setup(e => e.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());
            _applicationServiceMock
                .Setup(a => a.GetMonthlyCountsAsync(UserId, It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<string, int>());

            var result = await _service.GetMonthlyTrendsAsync(UserId, 1);

            Assert.That(result.Points[0].Enrolment, Is.EqualTo(0));
        }
    }
}
