using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.VisaProcess;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class VisaProcessTemplateServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IVisaProcessTemplate> _templateDbMock;
        private Mock<IValidator<VisaProcessTemplateModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<VisaProcessTemplateService>> _loggerMock;
        private VisaProcessTemplateService _service;

        [SetUp]
        public void Setup()
        {
            _templateDbMock = new Mock<IVisaProcessTemplate>();
            _validatorMock = new Mock<IValidator<VisaProcessTemplateModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<VisaProcessTemplateService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<VisaProcessTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.VisaProcessTemplatesEdit.GetDescription() });

            _templateDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<VisaProcessTemplateModel>());

            _service = new VisaProcessTemplateService(
                _templateDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetAllAsync_ReturnsTemplates_NoPermissionRequired()
        {
            _templateDbMock.Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<VisaProcessTemplateModel> { new() { Id = "1", Name = "Skilled Visa" } });

            var result = await _service.GetAllAsync();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetByIdAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetByIdAsync("1", UserId));
        }

        [Test]
        public async Task CreateAsync_NormalizesStepOrder_WhenCreated()
        {
            var template = new VisaProcessTemplateModel
            {
                Name = "Skilled Visa",
                Country = "AU",
                Category = "Skilled",
                Steps = new List<VisaProcessStepDefinitionModel>
                {
                    new() { Key = "s1", Order = 5 },
                    new() { Key = "s2", Order = 9 }
                }
            };

            var result = await _service.CreateAsync(template, UserId);

            Assert.That(result.Steps[0].Order, Is.EqualTo(0));
            Assert.That(result.Steps[1].Order, Is.EqualTo(1));
            Assert.That(result.Version, Is.EqualTo(1));
        }

        [Test]
        public void CreateAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<VisaProcessTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var template = new VisaProcessTemplateModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(template, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public async Task CreateAsync_ClearsOtherDefaults_WhenSetAsDefaultForCountry()
        {
            var otherDefault = new VisaProcessTemplateModel
            {
                Id = "existing-1", Country = "AU", Category = "Skilled", IsDefaultForCountry = true
            };
            _templateDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<VisaProcessTemplateModel> { otherDefault });
            _templateDbMock.Setup(db => db.ReplaceAsync("existing-1", It.IsAny<VisaProcessTemplateModel>())).ReturnsAsync(true);

            var template = new VisaProcessTemplateModel
            {
                Name = "New default", Country = "AU", Category = "Skilled", IsDefaultForCountry = true
            };

            await _service.CreateAsync(template, UserId);

            Assert.That(otherDefault.IsDefaultForCountry, Is.False);
            _templateDbMock.Verify(db => db.ReplaceAsync("existing-1", It.IsAny<VisaProcessTemplateModel>()), Times.Once);
        }

        [Test]
        public void UpdateAsync_Throws_WhenNotFound()
        {
            _templateDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((VisaProcessTemplateModel)null);

            var template = new VisaProcessTemplateModel { Name = "Skilled Visa" };

            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync("missing", template, UserId));
        }

        [Test]
        public async Task UpdateAsync_IncrementsVersion_WhenUpdated()
        {
            var existing = new VisaProcessTemplateModel { Id = "1", Name = "Old", Version = 3 };
            _templateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _templateDbMock.Setup(db => db.ReplaceAsync("1", It.IsAny<VisaProcessTemplateModel>())).ReturnsAsync(true);

            var updateDto = new VisaProcessTemplateModel { Name = "New" };

            var result = await _service.UpdateAsync("1", updateDto, UserId);

            Assert.That(result, Is.True);
            Assert.That(existing.Version, Is.EqualTo(4));
        }

        [Test]
        public async Task SetStatusAsync_SetsInactive_WhenDeactivating()
        {
            var existing = new VisaProcessTemplateModel { Id = "1", Name = "Skilled Visa", Status = "Active" };
            _templateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _templateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<VisaProcessTemplateModel>(t => t.Status == "Inactive")))
                .ReturnsAsync(true);

            var result = await _service.SetStatusAsync("1", false, UserId);

            Assert.That(result, Is.True);
        }
    }
}
