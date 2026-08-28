using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Enrolment;
using ShareService.Models.Setting;
using ShareService.Models.Settings;
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
    public class EnrolmentServiceTests
    {
        private const string ActingUserId = "staff-1";

        private Mock<IEnrolment> _enrolmentDbMock;
        private Mock<IApplication> _applicationDbMock;
        private Mock<IEnquiryService> _enquiryServiceMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IStudentService> _studentServiceMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IAzureEmailService> _emailServiceMock;
        private Mock<IAzureBlobDocStorageService> _blobStorageServiceMock;
        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<ICourse> _courseDbMock;
        private Mock<ISettings> _settingsDbMock;
        private Mock<IFinancialRecordService> _financialRecordServiceMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<IDynamicFormTemplate> _dynamicFormTemplateDbMock;
        private Mock<IEmailTemplate> _emailTemplateDbMock;
        private Mock<IInvoicePdfService> _pdfServiceMock;
        private Mock<IValidator<EnrolmentModel>> _validatorMock;
        private Mock<IOptions<DocumentLinkSettings>> _documentLinkSettingsMock;
        private Mock<IOptions<WebURLSettings>> _webUrlSettingsMock;
        private Mock<ILogger<EnrolmentService>> _loggerMock;
        private EnrolmentService _service;

        [SetUp]
        public void Setup()
        {
            _enrolmentDbMock = new Mock<IEnrolment>();
            _applicationDbMock = new Mock<IApplication>();
            _enquiryServiceMock = new Mock<IEnquiryService>();
            _userServiceMock = new Mock<IUserService>();
            _studentServiceMock = new Mock<IStudentService>();
            _roleServiceMock = new Mock<IRoleService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _emailServiceMock = new Mock<IAzureEmailService>();
            _blobStorageServiceMock = new Mock<IAzureBlobDocStorageService>();
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _courseDbMock = new Mock<ICourse>();
            _settingsDbMock = new Mock<ISettings>();
            _financialRecordServiceMock = new Mock<IFinancialRecordService>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _dynamicFormTemplateDbMock = new Mock<IDynamicFormTemplate>();
            _emailTemplateDbMock = new Mock<IEmailTemplate>();
            _pdfServiceMock = new Mock<IInvoicePdfService>();
            _validatorMock = new Mock<IValidator<EnrolmentModel>>();
            _documentLinkSettingsMock = new Mock<IOptions<DocumentLinkSettings>>();
            _webUrlSettingsMock = new Mock<IOptions<WebURLSettings>>();
            _loggerMock = new Mock<ILogger<EnrolmentService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EnrolmentsAdd.GetDescription(),
                    PermissionKey.EnrolmentsView.GetDescription(),
                    PermissionKey.EnrolmentsEdit.GetDescription()
                });

            _documentLinkSettingsMock.Setup(o => o.Value).Returns(new DocumentLinkSettings { ExpiryDays = 7 });
            _webUrlSettingsMock.Setup(o => o.Value).Returns(new WebURLSettings { FrontendBaseUrl = "http://localhost:4200" });

            _settingsDbMock.Setup(db => db.GetSettingsAsync()).ReturnsAsync(new SettingsModel { MaxApplicationsPerStudent = 2 });

            _service = new EnrolmentService(
                _enrolmentDbMock.Object,
                _applicationDbMock.Object,
                _enquiryServiceMock.Object,
                _userServiceMock.Object,
                _studentServiceMock.Object,
                _roleServiceMock.Object,
                _permissionServiceMock.Object,
                _emailServiceMock.Object,
                _blobStorageServiceMock.Object,
                _educationPartnerDbMock.Object,
                _courseDbMock.Object,
                _settingsDbMock.Object,
                _financialRecordServiceMock.Object,
                _notificationPublisherMock.Object,
                _dynamicFormTemplateDbMock.Object,
                _emailTemplateDbMock.Object,
                _pdfServiceMock.Object,
                _validatorMock.Object,
                _documentLinkSettingsMock.Object,
                _webUrlSettingsMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public void CreateIndependentAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            var input = new EnrolmentModel { FirstName = "Jane", LastName = "Doe" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateIndependentAsync(input, null, ActingUserId));
        }

        [Test]
        public void CreateIndependentAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Email", "Email is required")
                }));

            var input = new EnrolmentModel { FirstName = "Jane", LastName = "Doe" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateIndependentAsync(input, null, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void GetEnrolmentsAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetEnrolmentsAsync(new EnrolmentFilter(), ActingUserId));
        }

        [Test]
        public void UpdateEnrolmentAsync_Throws_WhenEnrolmentNotFound()
        {
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("missing")).ReturnsAsync((EnrolmentModel)null);

            var updateModel = new EnrolmentModel { FirstName = "Jane", LastName = "Doe" };

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateEnrolmentAsync("missing", updateModel, ActingUserId));
        }

        [Test]
        public void UpdateEnrolmentAsync_Throws_WhenCallerIsNotOwner()
        {
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1"))
                .ReturnsAsync(new EnrolmentModel { Id = "1", OwnerUserId = "someone-else" });

            var updateModel = new EnrolmentModel { FirstName = "Jane", LastName = "Doe" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateEnrolmentAsync("1", updateModel, ActingUserId));
        }

        [Test]
        public void SetMaxApplicationsOverrideAsync_Throws_WhenExceedsGlobalCeiling()
        {
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1"))
                .ReturnsAsync(new EnrolmentModel { Id = "1", OwnerUserId = ActingUserId });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SetMaxApplicationsOverrideAsync("1", 5, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("can't exceed the system maximum"));
        }

        [Test]
        public void FinalizeCourseApplicationAsync_Throws_WhenStatusNotOffered()
        {
            var enrolment = new EnrolmentModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                CourseApplications = new List<CourseApplicationModel>
                {
                    new() { Id = "ca1", Status = CourseApplicationStatuses.Init }
                }
            };
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1")).ReturnsAsync(enrolment);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.FinalizeCourseApplicationAsync("1", "ca1", ActingUserId));

            Assert.That(ex!.Message, Does.Contain("\"Offered\""));
        }

        [Test]
        public void FinalizeCourseApplicationAsync_Throws_WhenOfferLetterMissing()
        {
            var enrolment = new EnrolmentModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                CourseApplications = new List<CourseApplicationModel>
                {
                    new() { Id = "ca1", Status = CourseApplicationStatuses.Offered }
                },
                Documents = new List<EnrolmentDocumentModel>()
            };
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1")).ReturnsAsync(enrolment);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.FinalizeCourseApplicationAsync("1", "ca1", ActingUserId));

            Assert.That(ex!.Message, Does.Contain("offer letter"));
        }

        [Test]
        public void CompleteVisaStepAsync_Throws_WhenStepLocked()
        {
            var enrolment = new EnrolmentModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                VisaProcessSteps = new List<VisaProcessStepModel>
                {
                    new() { Key = VisaProcessStepKeys.CoeCompletion, Status = "Locked" }
                }
            };
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1")).ReturnsAsync(enrolment);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CompleteVisaStepAsync("1", VisaProcessStepKeys.CoeCompletion, new Dictionary<string, string>(), ActingUserId));

            Assert.That(ex!.Message, Does.Contain("locked"));
        }

        [Test]
        public void ReassignOwnerAsync_Throws_WhenCallerIsNeitherOwnerNorManager()
        {
            _enrolmentDbMock.Setup(db => db.GetEnrolmentAsync("1"))
                .ReturnsAsync(new EnrolmentModel { Id = "1", OwnerUserId = "someone-else" });
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.ReassignOwnerAsync("1", "new-owner", ActingUserId));
        }
    }
}
