using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.VisaProcess;
using ShareService.Models.MigrationCase;
using ShareService.Models.Setting;
using ShareService.Models.VisaProcess;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class MigrationCaseServiceTests
    {
        private const string ActingUserId = "staff-1";

        private Mock<IMigrationCase> _caseDbMock;
        private Mock<IVisaProcessTemplate> _templateDbMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IAzureBlobDocStorageService> _blobStorageServiceMock;
        private Mock<IAzureEmailService> _emailServiceMock;
        private Mock<IDynamicFormTemplate> _dynamicFormTemplateDbMock;
        private Mock<IInvoicePdfService> _pdfServiceMock;
        private Mock<IOptions<DocumentLinkSettings>> _documentLinkSettingsMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<ILogger<MigrationCaseService>> _loggerMock;
        private MigrationCaseService _service;

        [SetUp]
        public void Setup()
        {
            _caseDbMock = new Mock<IMigrationCase>();
            _templateDbMock = new Mock<IVisaProcessTemplate>();
            _userServiceMock = new Mock<IUserService>();
            _roleServiceMock = new Mock<IRoleService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _blobStorageServiceMock = new Mock<IAzureBlobDocStorageService>();
            _emailServiceMock = new Mock<IAzureEmailService>();
            _dynamicFormTemplateDbMock = new Mock<IDynamicFormTemplate>();
            _pdfServiceMock = new Mock<IInvoicePdfService>();
            _documentLinkSettingsMock = new Mock<IOptions<DocumentLinkSettings>>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _loggerMock = new Mock<ILogger<MigrationCaseService>>();

            _documentLinkSettingsMock.Setup(o => o.Value).Returns(new DocumentLinkSettings { ExpiryDays = 7 });

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.MigrationCasesView.GetDescription(),
                    PermissionKey.MigrationCasesAdd.GetDescription(),
                    PermissionKey.MigrationCasesEdit.GetDescription()
                });

            _caseDbMock.Setup(db => db.CountAllAsync()).ReturnsAsync(0);

            _service = new MigrationCaseService(
                _caseDbMock.Object,
                _templateDbMock.Object,
                _userServiceMock.Object,
                _roleServiceMock.Object,
                _permissionServiceMock.Object,
                _blobStorageServiceMock.Object,
                _emailServiceMock.Object,
                _dynamicFormTemplateDbMock.Object,
                _pdfServiceMock.Object,
                _documentLinkSettingsMock.Object,
                _notificationPublisherMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public void CreateCaseAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateCaseAsync("t1", "Jane Doe", null, null, null, ActingUserId));
        }

        [Test]
        public void CreateCaseAsync_Throws_WhenPrimaryContactNameEmpty()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateCaseAsync("t1", "", null, null, null, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("Primary contact name"));
        }

        [Test]
        public void CreateCaseAsync_Throws_WhenTemplateInactive()
        {
            _templateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new VisaProcessTemplateModel { Id = "t1", Status = VisaTemplateStatus.Inactive.ToString() });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateCaseAsync("t1", "Jane Doe", null, null, null, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("inactive"));
        }

        [Test]
        public void CreateCaseAsync_Throws_WhenNoEnabledSteps()
        {
            _templateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new VisaProcessTemplateModel
                {
                    Id = "t1",
                    Status = VisaTemplateStatus.Active.ToString(),
                    Steps = new List<VisaProcessStepDefinitionModel> { new() { Key = "s1", Enabled = false } }
                });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateCaseAsync("t1", "Jane Doe", null, null, null, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("no enabled steps"));
        }

        [Test]
        public async Task CreateCaseAsync_SnapshotsOnlyEnabledSteps()
        {
            _templateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new VisaProcessTemplateModel
                {
                    Id = "t1",
                    Name = "Skilled Visa",
                    Status = VisaTemplateStatus.Active.ToString(),
                    Steps = new List<VisaProcessStepDefinitionModel>
                    {
                        new() { Key = "s1", Order = 0, Label = "Step 1", Enabled = true },
                        new() { Key = "s2", Order = 1, Label = "Step 2", Enabled = false },
                        new() { Key = "s3", Order = 2, Label = "Step 3", Enabled = true }
                    }
                });
            _caseDbMock.Setup(db => db.CreateCaseAsync(It.IsAny<MigrationCaseModel>())).ReturnsAsync(true);

            var result = await _service.CreateCaseAsync("t1", "Jane Doe", "jane@b.com", null, null, ActingUserId);

            // Consultation step + the 2 enabled template steps.
            Assert.That(result.Steps, Has.Count.EqualTo(3));
            Assert.That(result.Steps.Select(s => s.Key), Does.Not.Contain("s2"));
        }

        [Test]
        public void SaveStepDraftAsync_Throws_WhenStepLocked()
        {
            var migrationCase = new MigrationCaseModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                Steps = new List<MigrationCaseStepModel> { new() { Key = "s1", Status = "Locked" } }
            };
            _caseDbMock.Setup(db => db.GetCaseAsync("1")).ReturnsAsync(migrationCase);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SaveStepDraftAsync("1", "s1", new Dictionary<string, string>(), ActingUserId));

            Assert.That(ex!.Message, Does.Contain("locked"));
        }

        [Test]
        public void CompleteStepAsync_Throws_WhenRequiredEvidenceMissing()
        {
            var migrationCase = new MigrationCaseModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                Steps = new List<MigrationCaseStepModel>
                {
                    new() { Key = "s1", Order = 0, Status = "Draft", RequiredEvidenceCategoriesSnapshot = new List<string> { "Passport" } }
                },
                Documents = new List<MigrationCaseDocumentModel>()
            };
            _caseDbMock.Setup(db => db.GetCaseAsync("1")).ReturnsAsync(migrationCase);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CompleteStepAsync("1", "s1", new Dictionary<string, string>(), ActingUserId));

            Assert.That(ex!.Message, Does.Contain("Passport"));
        }

        [Test]
        public async Task CompleteStepAsync_MarksCaseCompleted_WhenLastStepDone()
        {
            var migrationCase = new MigrationCaseModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                Status = "Active",
                Steps = new List<MigrationCaseStepModel>
                {
                    new() { Key = "s1", Order = 0, Status = "Draft" }
                },
                Documents = new List<MigrationCaseDocumentModel>()
            };
            _caseDbMock.Setup(db => db.GetCaseAsync("1")).ReturnsAsync(migrationCase);
            _caseDbMock.Setup(db => db.ReplaceCaseAsync("1", It.IsAny<MigrationCaseModel>())).ReturnsAsync(true);

            var result = await _service.CompleteStepAsync("1", "s1", new Dictionary<string, string>(), ActingUserId);

            Assert.That(result, Is.True);
            Assert.That(migrationCase.Status, Is.EqualTo("Completed"));
        }

        [Test]
        public void ReopenStepAsync_Throws_WhenStepCannotReopen()
        {
            var migrationCase = new MigrationCaseModel
            {
                Id = "1",
                OwnerUserId = ActingUserId,
                Steps = new List<MigrationCaseStepModel> { new() { Key = "s1", Status = "Complete", CanReopen = false } }
            };
            _caseDbMock.Setup(db => db.GetCaseAsync("1")).ReturnsAsync(migrationCase);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReopenStepAsync("1", "s1", ActingUserId));

            Assert.That(ex!.Message, Does.Contain("doesn't support reopening"));
        }

        [Test]
        public void ReassignOwnerAsync_Throws_WhenCallerIsNeitherOwnerNorManager()
        {
            _caseDbMock.Setup(db => db.GetCaseAsync("1"))
                .ReturnsAsync(new MigrationCaseModel { Id = "1", OwnerUserId = "someone-else" });
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.ReassignOwnerAsync("1", "new-owner", ActingUserId));
        }
    }
}
