using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Enquiry;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class EnquiryServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IEnquiry> _enquiryDbMock;
        private Mock<IRecaptchaService> _recaptchaServiceMock;
        private Mock<IValidator<EnquiryModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<ILogger<EnquiryService>> _loggerMock;
        private EnquiryService _service;

        [SetUp]
        public void Setup()
        {
            _enquiryDbMock = new Mock<IEnquiry>();
            _recaptchaServiceMock = new Mock<IRecaptchaService>();
            _validatorMock = new Mock<IValidator<EnquiryModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _loggerMock = new Mock<ILogger<EnquiryService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _recaptchaServiceMock
                .Setup(r => r.VerifyAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            _enquiryDbMock
                .Setup(db => db.GetEnquiryAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((EnquiryModel)null);

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EnquiryView.GetDescription(),
                    PermissionKey.EnquiryEdit.GetDescription(),
                    PermissionKey.EnquiryDelete.GetDescription()
                });

            _service = new EnquiryService(
                _enquiryDbMock.Object,
                _recaptchaServiceMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _notificationPublisherMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task CreateEnquiry_ReturnsTrue_AndNotifiesStaff_WhenCreated()
        {
            _enquiryDbMock
                .Setup(db => db.CreateEnquiryAsync(It.IsAny<EnquiryModel>()))
                .ReturnsAsync(true);

            var enquiry = new EnquiryModel { FirstName = "Jane", LastName = "Doe", Email = "jane@doe.com", Mobile = "123" };

            var result = await _service.CreateEnquiry(enquiry);

            Assert.That(result, Is.True);
            Assert.That(enquiry.Status, Is.EqualTo("New"));
            _notificationPublisherMock.Verify(n => n.PublishToRoleAsync(
                "Enquiry", It.IsAny<string>(), It.IsAny<string>(), ShareService.Enums.Roles.SystemRole.Staff), Times.Once);
        }

        [Test]
        public void CreateEnquiry_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Email", "Email is required")
                }));

            var enquiry = new EnquiryModel { FirstName = "Jane", LastName = "Doe", Email = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEnquiry(enquiry));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void CreateEnquiry_Throws_WhenRecaptchaFails()
        {
            _recaptchaServiceMock
                .Setup(r => r.VerifyAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var enquiry = new EnquiryModel { FirstName = "Jane", LastName = "Doe", Email = "jane@doe.com", Mobile = "123" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEnquiry(enquiry));

            Assert.That(ex!.Message, Does.Contain("reCAPTCHA"));
        }

        [Test]
        public void CreateEnquiry_Throws_WhenPendingEnquiryAlreadyExists()
        {
            _enquiryDbMock
                .Setup(db => db.GetEnquiryAsync("jane@doe.com", "123"))
                .ReturnsAsync(new EnquiryModel { Id = "1", Status = "New" });

            var enquiry = new EnquiryModel { FirstName = "Jane", LastName = "Doe", Email = "jane@doe.com", Mobile = "123" };

            Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEnquiry(enquiry));
        }

        [Test]
        public void GetEnquiries_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetEnquiries(new EnquiryFilter(), UserId));
        }

        [Test]
        public void UpdateEnquiriesAsync_Throws_WhenNotFound()
        {
            _enquiryDbMock
                .Setup(db => db.GetEnquiryAsync("missing"))
                .ReturnsAsync((EnquiryModel)null);

            var updateModel = new EnquiryModel { Id = "missing", FirstName = "Jane", LastName = "Doe" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateEnquiriesAsync("missing", updateModel, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public void UpdateEnquiriesAsync_Throws_WhenChangingExistingResponse()
        {
            var existing = new EnquiryModel { Id = "1", Response = "Original reply", Status = "Responded" };
            _enquiryDbMock.Setup(db => db.GetEnquiryAsync("1")).ReturnsAsync(existing);

            var updateModel = new EnquiryModel { Id = "1", Response = "Changed reply" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateEnquiriesAsync("1", updateModel, UserId));

            Assert.That(ex!.Message, Does.Contain("cannot be changed"));
        }

        [Test]
        public async Task DeleteEnquiriesAsync_ReturnsTrue_WhenDeleted()
        {
            _enquiryDbMock
                .Setup(db => db.DeleteEnquiriesAsync("1"))
                .ReturnsAsync(true);

            var result = await _service.DeleteEnquiriesAsync("1", UserId);

            Assert.That(result, Is.True);
        }
    }
}
