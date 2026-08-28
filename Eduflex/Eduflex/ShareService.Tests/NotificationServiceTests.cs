using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Models.Notification;
using ShareService.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private const string UserId = "user-1";

        private Mock<INotification> _notificationDbMock;
        private NotificationService _service;

        [SetUp]
        public void Setup()
        {
            _notificationDbMock = new Mock<INotification>();
            _service = new NotificationService(_notificationDbMock.Object);
        }

        [Test]
        public async Task GetMyNotificationsAsync_ReturnsNotifications_ForUser()
        {
            var notifications = new List<NotificationModel>
            {
                new() { Id = "1", Summary = "New enquiry" }
            };
            _notificationDbMock
                .Setup(db => db.GetActiveNotificationsForUserAsync(UserId))
                .ReturnsAsync(notifications);

            var result = await _service.GetMyNotificationsAsync(UserId);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Summary, Is.EqualTo("New enquiry"));
        }

        [Test]
        public async Task GetMyNotificationsAsync_ReturnsEmptyList_WhenNoneActive()
        {
            _notificationDbMock
                .Setup(db => db.GetActiveNotificationsForUserAsync(UserId))
                .ReturnsAsync(new List<NotificationModel>());

            var result = await _service.GetMyNotificationsAsync(UserId);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetMyNotificationsAsync_DelegatesUserIdToDataAccess()
        {
            _notificationDbMock
                .Setup(db => db.GetActiveNotificationsForUserAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<NotificationModel>());

            await _service.GetMyNotificationsAsync(UserId);

            _notificationDbMock.Verify(db => db.GetActiveNotificationsForUserAsync(UserId), Times.Once);
        }

        [Test]
        public async Task ClearNotificationAsync_ReturnsTrue_WhenCleared()
        {
            _notificationDbMock
                .Setup(db => db.ClearNotificationAsync("1", UserId))
                .ReturnsAsync(true);

            var result = await _service.ClearNotificationAsync("1", UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ClearNotificationAsync_ReturnsFalse_WhenNotFoundOrNotOwnedByUser()
        {
            _notificationDbMock
                .Setup(db => db.ClearNotificationAsync("missing", UserId))
                .ReturnsAsync(false);

            var result = await _service.ClearNotificationAsync("missing", UserId);

            Assert.That(result, Is.False);
        }
    }
}
