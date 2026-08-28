using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Settings;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private const string UserId = "user-1";

        private Mock<ISettings> _settingsDbMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<SettingsService>> _loggerMock;
        private SettingsService _service;

        private static SettingsModel ValidSettings() => new()
        {
            FeedbackDefaultLatestCount = 10,
            CoursePromotionDefaultLatestCount = 10,
            MaxApplicationsPerStudent = 1,
            DocumentUpload = new DocumentUploadSettings
            {
                Default = new UploadLimit { MaxSizeMB = 5, MaxFileCount = 1 },
                Other = new UploadLimit { MaxSizeMB = 5, MaxFileCount = 4 }
            },
            ImageUpload = new UploadLimit { MaxSizeMB = 2, MaxFileCount = 1 },
            ContractUpload = new UploadLimit { MaxSizeMB = 10, MaxFileCount = 1 },
            EnrolmentUpload = new UploadLimit { MaxSizeMB = 10, MaxFileCount = 1 },
            ChatSystemPrompt = "You are a helpful assistant.",
            ChatApiUrl = "https://example.com/chat",
            ChatGroqApiUrl = "https://example.com/groq",
            ChatOpenRouterApiUrl = "https://example.com/openrouter",
            ChatGeminiModel = "gemini-3.0",
            ChatGroqModel = "llama-3",
            ChatOpenRouterModel = "nemotron"
        };

        [SetUp]
        public void Setup()
        {
            _settingsDbMock = new Mock<ISettings>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<SettingsService>>();

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.SettingsEdit.GetDescription() });

            _service = new SettingsService(_settingsDbMock.Object, _permissionServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetSettingsAsync_ReturnsSettings_WhenInitialized()
        {
            _settingsDbMock.Setup(db => db.GetSettingsAsync()).ReturnsAsync(ValidSettings());

            var result = await _service.GetSettingsAsync();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetSettingsAsync_Throws_WhenNotInitialized()
        {
            _settingsDbMock.Setup(db => db.GetSettingsAsync()).ReturnsAsync((SettingsModel)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetSettingsAsync());
        }

        [Test]
        public void UpdateSettingsAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.UpdateSettingsAsync(ValidSettings(), UserId));
        }

        [Test]
        public void UpdateSettingsAsync_Throws_WhenFeedbackDefaultLatestCountNotPositive()
        {
            var settings = ValidSettings();
            settings.FeedbackDefaultLatestCount = 0;

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(settings, UserId));

            Assert.That(ex!.Message, Does.Contain("FeedbackDefaultLatestCount"));
        }

        [Test]
        public void UpdateSettingsAsync_Throws_WhenMaxApplicationsPerStudentNotPositive()
        {
            var settings = ValidSettings();
            settings.MaxApplicationsPerStudent = 0;

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(settings, UserId));

            Assert.That(ex!.Message, Does.Contain("MaxApplicationsPerStudent"));
        }

        [Test]
        public void UpdateSettingsAsync_Throws_WhenUploadLimitMaxSizeNotPositive()
        {
            var settings = ValidSettings();
            settings.ImageUpload.MaxSizeMB = 0;

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(settings, UserId));

            Assert.That(ex!.Message, Does.Contain("Image upload MaxSizeMB"));
        }

        [Test]
        public void UpdateSettingsAsync_Throws_WhenChatSystemPromptEmpty()
        {
            var settings = ValidSettings();
            settings.ChatSystemPrompt = "  ";

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(settings, UserId));

            Assert.That(ex!.Message, Does.Contain("ChatSystemPrompt"));
        }

        [Test]
        public async Task UpdateSettingsAsync_ReturnsUpdatedSettings_WhenValid()
        {
            var settings = ValidSettings();
            _settingsDbMock.Setup(db => db.UpsertSettingsAsync(settings)).ReturnsAsync(settings);

            var result = await _service.UpdateSettingsAsync(settings, UserId);

            Assert.That(result, Is.SameAs(settings));
            _settingsDbMock.Verify(db => db.UpsertSettingsAsync(settings), Times.Once);
        }
    }
}
