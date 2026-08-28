using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.BusinessPartner;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class BusinessPartnerServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IBusinessPartner> _businessPartnerDbMock;
        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<IValidator<BusinessPartnerModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<BusinessPartnerService>> _loggerMock;
        private BusinessPartnerService _service;

        [SetUp]
        public void Setup()
        {
            _businessPartnerDbMock = new Mock<IBusinessPartner>();
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _validatorMock = new Mock<IValidator<BusinessPartnerModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<BusinessPartnerService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<BusinessPartnerModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.BusinessPartnersView.GetDescription(),
                    PermissionKey.BusinessPartnersAdd.GetDescription(),
                    PermissionKey.BusinessPartnersEdit.GetDescription(),
                    PermissionKey.BusinessPartnersDelete.GetDescription()
                });

            _service = new BusinessPartnerService(
                _businessPartnerDbMock.Object,
                _educationPartnerDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetBusinessPartnerById_ReturnsPartner_WhenFound()
        {
            var partner = new BusinessPartnerModel { Id = "1", Name = "Acme" };
            _businessPartnerDbMock.Setup(db => db.GetBusinessPartnerByIdAsync("1")).ReturnsAsync(partner);

            var result = await _service.GetBusinessPartnerById("1", UserId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Acme"));
        }

        [Test]
        public void GetBusinessPartnerById_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetBusinessPartnerById("1", UserId));
        }

        [Test]
        public async Task CreateBusinessPartner_ReturnsTrue_WhenCreated()
        {
            _businessPartnerDbMock
                .Setup(db => db.CreateBusinessPartnerAsync(It.IsAny<BusinessPartnerModel>()))
                .ReturnsAsync(true);

            var partner = new BusinessPartnerModel { Name = "Acme", Email = "a@acme.com", PhoneNumber = "123" };

            var result = await _service.CreateBusinessPartner(partner, UserId);

            Assert.That(result, Is.True);
            Assert.That(partner.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateBusinessPartner_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<BusinessPartnerModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var partner = new BusinessPartnerModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateBusinessPartner(partner, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateBusinessPartner_Throws_WhenNotFound()
        {
            _businessPartnerDbMock
                .Setup(db => db.GetBusinessPartnerByIdAsync("missing"))
                .ReturnsAsync((BusinessPartnerModel)null);

            var partner = new BusinessPartnerModel { Name = "Acme", Email = "a@acme.com", PhoneNumber = "123" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateBusinessPartner("missing", partner, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task UpdateBusinessPartner_ReturnsTrue_WhenUpdated()
        {
            var existing = new BusinessPartnerModel { Id = "1", Name = "Old Name", Email = "old@acme.com", PhoneNumber = "111" };
            _businessPartnerDbMock.Setup(db => db.GetBusinessPartnerByIdAsync("1")).ReturnsAsync(existing);
            _businessPartnerDbMock
                .Setup(db => db.UpdateBusinessPartnerAsync("1", It.IsAny<BusinessPartnerModel>()))
                .ReturnsAsync(true);

            var updateDto = new BusinessPartnerModel { Name = "New Name", Email = "new@acme.com", PhoneNumber = "222" };

            var result = await _service.UpdateBusinessPartner("1", updateDto, UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public void DeleteBusinessPartner_Throws_WhenEducationPartnersStillLinked()
        {
            _educationPartnerDbMock
                .Setup(db => db.ExistsWithBusinessPartnerIdAsync("1"))
                .ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DeleteBusinessPartner("1", UserId));

            Assert.That(ex!.Message, Does.Contain("managed under"));
            _businessPartnerDbMock.Verify(db => db.DeleteBusinessPartnerAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task DeleteBusinessPartner_ReturnsTrue_WhenDeleted()
        {
            _educationPartnerDbMock
                .Setup(db => db.ExistsWithBusinessPartnerIdAsync("1"))
                .ReturnsAsync(false);
            _businessPartnerDbMock
                .Setup(db => db.DeleteBusinessPartnerAsync("1"))
                .ReturnsAsync(true);

            var result = await _service.DeleteBusinessPartner("1", UserId);

            Assert.That(result, Is.True);
        }
    }
}
