using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Feedback;
using ShareService.Models.Settings;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class FeedbackServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IFeedback> _feedbackDbMock;
        private Mock<IValidator<FeedbackModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ISettingsService> _settingsServiceMock;
        private Mock<IDistributedCache> _cacheMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<ILogger<FeedbackService>> _loggerMock;
        private FeedbackService _service;

        [SetUp]
        public void Setup()
        {
            _feedbackDbMock = new Mock<IFeedback>();
            _validatorMock = new Mock<IValidator<FeedbackModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _settingsServiceMock = new Mock<ISettingsService>();
            _cacheMock = new Mock<IDistributedCache>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _loggerMock = new Mock<ILogger<FeedbackService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<FeedbackModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.FeedbackView.GetDescription(),
                    PermissionKey.FeedbackDelete.GetDescription()
                });

            _settingsServiceMock
                .Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(new SettingsModel { FeedbackDefaultLatestCount = 10 });

            _cacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _cacheMock
                .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _cacheMock
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _service = new FeedbackService(
                _feedbackDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _settingsServiceMock.Object,
                _cacheMock.Object,
                _notificationPublisherMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task CreateFeedback_ReturnsTrue_AndNotifiesStaff_WhenCreated()
        {
            _feedbackDbMock
                .Setup(db => db.CreateFeedbackAsync(It.IsAny<FeedbackModel>()))
                .ReturnsAsync(true);

            var feedback = new FeedbackModel { Name = "Jane" };

            var result = await _service.CreateFeedback(feedback);

            Assert.That(result, Is.True);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _notificationPublisherMock.Verify(n => n.PublishToRoleAsync(
                "Feedback", It.IsAny<string>(), It.IsAny<string>(), ShareService.Enums.Roles.SystemRole.Staff), Times.Once);
        }

        [Test]
        public void CreateFeedback_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<FeedbackModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var feedback = new FeedbackModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateFeedback(feedback));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public async Task GetLatestFeedback_FetchesFromDb_WhenCacheMiss()
        {
            var feedbackList = new List<FeedbackModel> { new() { Id = "1", Name = "Jane" } };
            _feedbackDbMock
                .Setup(db => db.GetLatestFeedbackAsync(10))
                .ReturnsAsync(feedbackList);

            var result = await _service.GetLatestFeedback(5);

            Assert.That(result, Has.Count.EqualTo(1));
            _feedbackDbMock.Verify(db => db.GetLatestFeedbackAsync(10), Times.Once);
        }

        [Test]
        public async Task GetLatestFeedback_RefetchesFromDb_WhenCachedCountBelowRequested()
        {
            _settingsServiceMock
                .Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(new SettingsModel { FeedbackDefaultLatestCount = 10 });

            var freshList = new List<FeedbackModel> { new() { Id = "1" }, new() { Id = "2" } };
            _feedbackDbMock
                .Setup(db => db.GetLatestFeedbackAsync(15))
                .ReturnsAsync(freshList);

            var result = await _service.GetLatestFeedback(15);

            Assert.That(result, Has.Count.EqualTo(2));
            _feedbackDbMock.Verify(db => db.GetLatestFeedbackAsync(15), Times.Once);
        }

        [Test]
        public void GetFeedback_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetFeedback(new PaginationQuery(), UserId));
        }

        [Test]
        public async Task DeleteFeedback_ReturnsTrue_AndInvalidatesCache_WhenDeleted()
        {
            _feedbackDbMock
                .Setup(db => db.DeleteFeedbackAsync("1"))
                .ReturnsAsync(true);

            var result = await _service.DeleteFeedback("1", UserId);

            Assert.That(result, Is.True);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void DeleteFeedback_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DeleteFeedback("1", UserId));
        }
    }
}
