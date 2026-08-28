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
    public class PractitionerTagServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IPractitionerTag> _tagDbMock;
        private Mock<IValidator<PractitionerTagModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<PractitionerTagService>> _loggerMock;
        private PractitionerTagService _service;

        [SetUp]
        public void Setup()
        {
            _tagDbMock = new Mock<IPractitionerTag>();
            _validatorMock = new Mock<IValidator<PractitionerTagModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<PractitionerTagService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<PractitionerTagModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.VisaProcessTemplatesEdit.GetDescription() });

            _service = new PractitionerTagService(
                _tagDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetAllAsync_ReturnsTags_NoPermissionRequired()
        {
            _tagDbMock.Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<PractitionerTagModel> { new() { Id = "1", Name = "MARA Agent" } });

            var result = await _service.GetAllAsync();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void CreateAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            var tag = new PractitionerTagModel { Name = "MARA Agent" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateAsync(tag, UserId));
        }

        [Test]
        public async Task CreateAsync_ReturnsTag_WhenCreated()
        {
            var tag = new PractitionerTagModel { Name = "MARA Agent" };

            var result = await _service.CreateAsync(tag, UserId);

            Assert.That(result.Id, Is.EqualTo(string.Empty));
            _tagDbMock.Verify(db => db.CreateAsync(It.IsAny<PractitionerTagModel>()), Times.Once);
        }

        [Test]
        public void CreateAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<PractitionerTagModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var tag = new PractitionerTagModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(tag, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateAsync_Throws_WhenNotFound()
        {
            _tagDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((PractitionerTagModel)null);

            var tag = new PractitionerTagModel { Name = "MARA Agent" };

            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync("missing", tag, UserId));
        }

        [Test]
        public async Task UpdateAsync_ReturnsTrue_WhenUpdated()
        {
            var existing = new PractitionerTagModel { Id = "1", Name = "Old" };
            _tagDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _tagDbMock.Setup(db => db.ReplaceAsync("1", It.IsAny<PractitionerTagModel>())).ReturnsAsync(true);

            var updateDto = new PractitionerTagModel { Name = "New" };

            var result = await _service.UpdateAsync("1", updateDto, UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SetActiveAsync_SetsActiveFalse_WhenDeactivating()
        {
            var existing = new PractitionerTagModel { Id = "1", Name = "MARA Agent", Active = true };
            _tagDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _tagDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<PractitionerTagModel>(t => t.Active == false)))
                .ReturnsAsync(true);

            var result = await _service.SetActiveAsync("1", false, UserId);

            Assert.That(result, Is.True);
        }
    }
}
