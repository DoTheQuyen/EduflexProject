using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.CoursePromotion;
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
    public class CoursePromotionServiceTests
    {
        private const string UserId = "user-1";

        private Mock<ICoursePromotion> _coursePromotionDbMock;
        private Mock<IValidator<CoursePromotionModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ISettingsService> _settingsServiceMock;
        private Mock<IDistributedCache> _cacheMock;
        private Mock<ILogger<CoursePromotionService>> _loggerMock;
        private CoursePromotionService _service;

        [SetUp]
        public void Setup()
        {
            _coursePromotionDbMock = new Mock<ICoursePromotion>();
            _validatorMock = new Mock<IValidator<CoursePromotionModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _settingsServiceMock = new Mock<ISettingsService>();
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<CoursePromotionService>>();

            // Constructor reads settings synchronously (.Result) to seed the featured-cache max count.
            _settingsServiceMock
                .Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(new SettingsModel { CoursePromotionDefaultLatestCount = 10 });

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CoursePromotionModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.CoursePromotionsAdd.GetDescription(),
                    PermissionKey.CoursePromotionsView.GetDescription(),
                    PermissionKey.CoursePromotionsEdit.GetDescription(),
                    PermissionKey.CoursePromotionsDelete.GetDescription()
                });

            // Cache miss by default; SetAsync/RemoveAsync no-op.
            _cacheMock
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _cacheMock
                .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _cacheMock
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _service = new CoursePromotionService(
                _coursePromotionDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _settingsServiceMock.Object,
                _cacheMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public void CreateCoursePromotion_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            var promotion = new CoursePromotionModel { CourseName = "Course" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateCoursePromotion(promotion, UserId));
        }

        [Test]
        public void CreateCoursePromotion_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CoursePromotionModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("CourseName", "CourseName is required")
                }));

            var promotion = new CoursePromotionModel { CourseName = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateCoursePromotion(promotion, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public async Task CreateCoursePromotion_ReturnsTrue_AndInvalidatesCache_WhenCreated()
        {
            _coursePromotionDbMock
                .Setup(db => db.CreateCoursePromotionAsync(It.IsAny<CoursePromotionModel>()))
                .ReturnsAsync(true);

            var promotion = new CoursePromotionModel { CourseName = "Course" };

            var result = await _service.CreateCoursePromotion(promotion, UserId);

            Assert.That(result, Is.True);
            Assert.That(promotion.Id, Is.EqualTo(string.Empty));
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetFeaturedActiveCoursePromotions_FetchesFromDb_WhenCacheMiss()
        {
            var promotions = new List<CoursePromotionModel> { new() { Id = "1", CourseName = "Course" } };
            _coursePromotionDbMock
                .Setup(db => db.GetFeaturedActiveCoursePromotionsAsync(10))
                .ReturnsAsync(promotions);

            var result = await _service.GetFeaturedActiveCoursePromotions(5);

            Assert.That(result, Has.Count.EqualTo(1));
            _coursePromotionDbMock.Verify(db => db.GetFeaturedActiveCoursePromotionsAsync(10), Times.Once);
        }

        [Test]
        public async Task GetFeaturedActiveCoursePromotions_BypassesCache_WhenCountExceedsMax()
        {
            var promotions = new List<CoursePromotionModel> { new() { Id = "1", CourseName = "Course" } };
            _coursePromotionDbMock
                .Setup(db => db.GetFeaturedActiveCoursePromotionsAsync(20))
                .ReturnsAsync(promotions);

            var result = await _service.GetFeaturedActiveCoursePromotions(20);

            Assert.That(result, Has.Count.EqualTo(1));
            _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void GetCoursePromotions_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetCoursePromotions(new CoursePromotionFilter(), UserId));
        }

        [Test]
        public void UpdateCoursePromotion_Throws_WhenNotFound()
        {
            _coursePromotionDbMock
                .Setup(db => db.GetCoursePromotionByIdAsync("missing"))
                .ReturnsAsync((CoursePromotionModel)null);

            var promotion = new CoursePromotionModel { CourseName = "Course" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateCoursePromotion("missing", promotion, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task DeleteCoursePromotion_ReturnsTrue_AndInvalidatesCache_WhenDeleted()
        {
            _coursePromotionDbMock
                .Setup(db => db.DeleteCoursePromotionAsync("1"))
                .ReturnsAsync(true);

            var result = await _service.DeleteCoursePromotion("1", UserId);

            Assert.That(result, Is.True);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
