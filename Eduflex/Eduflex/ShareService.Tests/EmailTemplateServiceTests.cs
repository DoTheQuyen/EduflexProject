using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Enrolment;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class EmailTemplateServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IEmailTemplate> _emailTemplateDbMock;
        private Mock<IValidator<EmailTemplateModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<EmailTemplateService>> _loggerMock;
        private EmailTemplateService _service;

        [SetUp]
        public void Setup()
        {
            _emailTemplateDbMock = new Mock<IEmailTemplate>();
            _validatorMock = new Mock<IValidator<EmailTemplateModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<EmailTemplateService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<EmailTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.EmailTemplatesEdit.GetDescription() });

            _emailTemplateDbMock
                .Setup(db => db.GetByKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((EmailTemplateModel)null);

            _service = new EmailTemplateService(
                _emailTemplateDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetAllAsync_ReturnsTemplates_NoPermissionRequired()
        {
            _emailTemplateDbMock
                .Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<EmailTemplateModel> { new() { Id = "1", Key = "welcome" } });

            var result = await _service.GetAllAsync();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetByIdAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync("1", UserId));
        }

        [Test]
        public async Task CreateAsync_SetsDefaults_WhenCreated()
        {
            var template = new EmailTemplateModel { Key = "welcome", Subject = "Hi" };

            var result = await _service.CreateAsync(template, UserId);

            Assert.That(result.Id, Is.EqualTo(string.Empty));
            Assert.That(result.IsSystemDefault, Is.False);
            Assert.That(result.IsActive, Is.True);
            _emailTemplateDbMock.Verify(db => db.CreateAsync(It.IsAny<EmailTemplateModel>()), Times.Once);
        }

        [Test]
        public void CreateAsync_Throws_WhenKeyAlreadyExists()
        {
            _emailTemplateDbMock
                .Setup(db => db.GetByKeyAsync("welcome"))
                .ReturnsAsync(new EmailTemplateModel { Id = "1", Key = "welcome" });

            var template = new EmailTemplateModel { Key = "welcome", Subject = "Hi" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(template, UserId));

            Assert.That(ex!.Message, Does.Contain("already exists"));
        }

        [Test]
        public void CreateAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<EmailTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Subject", "Subject is required")
                }));

            var template = new EmailTemplateModel { Key = "welcome", Subject = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(template, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateAsync_Throws_WhenNotFound()
        {
            _emailTemplateDbMock
                .Setup(db => db.GetByIdAsync("missing"))
                .ReturnsAsync((EmailTemplateModel)null);

            var template = new EmailTemplateModel { Subject = "Hi" };

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync("missing", template, UserId));
        }

        [Test]
        public async Task UpdateAsync_PreservesExistingKey_WhenUpdated()
        {
            var existing = new EmailTemplateModel { Id = "1", Key = "welcome", Subject = "Old" };
            _emailTemplateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _emailTemplateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<EmailTemplateModel>(t => t.Key == "welcome")))
                .ReturnsAsync(true);

            var updateDto = new EmailTemplateModel { Subject = "New" };

            var result = await _service.UpdateAsync("1", updateDto, UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SetStatusAsync_SetsIsActive_WhenDeactivating()
        {
            var existing = new EmailTemplateModel { Id = "1", Key = "welcome", IsActive = true };
            _emailTemplateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _emailTemplateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<EmailTemplateModel>(t => t.IsActive == false)))
                .ReturnsAsync(true);

            var result = await _service.SetStatusAsync("1", false, UserId);

            Assert.That(result, Is.True);
        }
    }
}
