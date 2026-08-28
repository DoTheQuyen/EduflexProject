using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.EducationPartner;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class EducationPartnerServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<ICourse> _courseDbMock;
        private Mock<IValidator<EducationPartnerModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<EducationPartnerService>> _loggerMock;
        private EducationPartnerService _service;

        [SetUp]
        public void Setup()
        {
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _courseDbMock = new Mock<ICourse>();
            _validatorMock = new Mock<IValidator<EducationPartnerModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<EducationPartnerService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<EducationPartnerModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EducationPartnersView.GetDescription(),
                    PermissionKey.EducationPartnersAdd.GetDescription(),
                    PermissionKey.EducationPartnersEdit.GetDescription(),
                    PermissionKey.EducationPartnersDelete.GetDescription()
                });

            _service = new EducationPartnerService(
                _educationPartnerDbMock.Object,
                _courseDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetEducationPartnerById_ReturnsPartner_WhenFound()
        {
            var partner = new EducationPartnerModel { Id = "1", Name = "Acme Uni" };
            _educationPartnerDbMock.Setup(db => db.GetEducationPartnerByIdAsync("1")).ReturnsAsync(partner);

            var result = await _service.GetEducationPartnerById("1", UserId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Acme Uni"));
        }

        [Test]
        public void GetEducationPartnerById_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetEducationPartnerById("1", UserId));
        }

        [Test]
        public async Task GetEducationPartners_ReturnsResults_NoPermissionRequired()
        {
            _educationPartnerDbMock
                .Setup(db => db.GetEducationPartnersAsync(It.IsAny<EducationPartnerFilter>()))
                .ReturnsAsync(new PagedResult<EducationPartnerModel> { Items = new List<EducationPartnerModel> { new() { Id = "1" } } });

            var result = await _service.GetEducationPartners(new EducationPartnerFilter());

            Assert.That(result.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void CreateEducationPartner_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<EducationPartnerModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var partner = new EducationPartnerModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateEducationPartner(partner, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public async Task CreateEducationPartner_ReturnsTrue_WhenCreated()
        {
            _educationPartnerDbMock
                .Setup(db => db.CreateEducationPartnerAsync(It.IsAny<EducationPartnerModel>()))
                .ReturnsAsync(true);

            var partner = new EducationPartnerModel { Name = "Acme Uni", Email = "a@acme.com", PhoneNumber = "123" };

            var result = await _service.CreateEducationPartner(partner, UserId);

            Assert.That(result, Is.True);
            Assert.That(partner.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void UpdateEducationPartner_Throws_WhenNotFound()
        {
            _educationPartnerDbMock
                .Setup(db => db.GetEducationPartnerByIdAsync("missing"))
                .ReturnsAsync((EducationPartnerModel)null);

            var partner = new EducationPartnerModel { Name = "Acme Uni" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateEducationPartner("missing", partner, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task DeleteEducationPartner_CascadesCourseDeletion_WhenDeleted()
        {
            _educationPartnerDbMock
                .Setup(db => db.DeleteEducationPartnerAsync("1"))
                .ReturnsAsync(true);

            var result = await _service.DeleteEducationPartner("1", UserId);

            Assert.That(result, Is.True);
            _courseDbMock.Verify(db => db.DeleteByPartnerIdAsync("1"), Times.Once);
        }

        [Test]
        public async Task DeleteEducationPartner_DoesNotCascade_WhenNotDeleted()
        {
            _educationPartnerDbMock
                .Setup(db => db.DeleteEducationPartnerAsync("1"))
                .ReturnsAsync(false);

            var result = await _service.DeleteEducationPartner("1", UserId);

            Assert.That(result, Is.False);
            _courseDbMock.Verify(db => db.DeleteByPartnerIdAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
