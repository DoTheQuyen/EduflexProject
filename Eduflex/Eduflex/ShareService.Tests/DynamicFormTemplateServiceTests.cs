using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.DynamicForm;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class DynamicFormTemplateServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IDynamicFormTemplate> _templateDbMock;
        private Mock<IValidator<DynamicFormTemplateModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<DynamicFormTemplateService>> _loggerMock;
        private DynamicFormTemplateService _service;

        [SetUp]
        public void Setup()
        {
            _templateDbMock = new Mock<IDynamicFormTemplate>();
            _validatorMock = new Mock<IValidator<DynamicFormTemplateModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<DynamicFormTemplateService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DynamicFormTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.DynamicFormsEdit.GetDescription() });

            _service = new DynamicFormTemplateService(
                _templateDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetAllAsync_ReturnsTemplates_NoPermissionRequired()
        {
            _templateDbMock
                .Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<DynamicFormTemplateModel> { new() { Id = "1", Name = "Form A" } });

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
        public async Task CreateAsync_AssignsQuestionOrderAndIds()
        {
            _templateDbMock
                .Setup(db => db.CreateAsync(It.IsAny<DynamicFormTemplateModel>()))
                .ReturnsAsync(true);

            var template = new DynamicFormTemplateModel
            {
                Name = "Form A",
                Questions = new List<FormQuestionModel>
                {
                    new() { Id = "", QuestionText = "Q1" },
                    new() { Id = "", QuestionText = "Q2" }
                }
            };

            var result = await _service.CreateAsync(template, UserId);

            Assert.That(result.Questions[0].Order, Is.EqualTo(0));
            Assert.That(result.Questions[1].Order, Is.EqualTo(1));
            Assert.That(result.Questions[0].Id, Is.Not.Empty);
        }

        [Test]
        public void CreateAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DynamicFormTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var template = new DynamicFormTemplateModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(template, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateAsync_Throws_WhenNotFound()
        {
            _templateDbMock
                .Setup(db => db.GetByIdAsync("missing"))
                .ReturnsAsync((DynamicFormTemplateModel)null);

            var template = new DynamicFormTemplateModel { Name = "Form A" };

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync("missing", template, UserId));
        }

        [Test]
        public async Task UpdateAsync_ReturnsTrue_WhenReplaced()
        {
            var existing = new DynamicFormTemplateModel { Id = "1", Name = "Old" };
            _templateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _templateDbMock
                .Setup(db => db.ReplaceAsync("1", It.IsAny<DynamicFormTemplateModel>()))
                .ReturnsAsync(true);

            var updateDto = new DynamicFormTemplateModel { Name = "New" };

            var result = await _service.UpdateAsync("1", updateDto, UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SetStatusAsync_SetsInactive_WhenDeactivating()
        {
            var existing = new DynamicFormTemplateModel { Id = "1", Name = "Form A", Status = "Active" };
            _templateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _templateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<DynamicFormTemplateModel>(t => t.Status == "Inactive")))
                .ReturnsAsync(true);

            var result = await _service.SetStatusAsync("1", false, UserId);

            Assert.That(result, Is.True);
        }
    }
}
