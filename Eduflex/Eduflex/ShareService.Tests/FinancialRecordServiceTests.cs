using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Enrolment;
using ShareService.Models.Financial;
using ShareService.Models.Setting;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class FinancialRecordServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IFinancialRecord> _financialRecordDbMock;
        private Mock<ICourse> _courseDbMock;
        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<IBusinessPartner> _businessPartnerDbMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IAzureBlobDocStorageService> _blobStorageServiceMock;
        private Mock<IAzureEmailService> _emailServiceMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<IInvoice> _invoiceDbMock;
        private Mock<IOptions<DocumentLinkSettings>> _documentLinkSettingsMock;
        private Mock<ILogger<FinancialRecordService>> _loggerMock;
        private FinancialRecordService _service;

        [SetUp]
        public void Setup()
        {
            _financialRecordDbMock = new Mock<IFinancialRecord>();
            _courseDbMock = new Mock<ICourse>();
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _businessPartnerDbMock = new Mock<IBusinessPartner>();
            _userServiceMock = new Mock<IUserService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _blobStorageServiceMock = new Mock<IAzureBlobDocStorageService>();
            _emailServiceMock = new Mock<IAzureEmailService>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _invoiceDbMock = new Mock<IInvoice>();
            _documentLinkSettingsMock = new Mock<IOptions<DocumentLinkSettings>>();
            _loggerMock = new Mock<ILogger<FinancialRecordService>>();

            _documentLinkSettingsMock.Setup(o => o.Value).Returns(new DocumentLinkSettings { ExpiryDays = 7 });

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.FinanceView.GetDescription(),
                    PermissionKey.FinanceEdit.GetDescription()
                });

            _service = new FinancialRecordService(
                _financialRecordDbMock.Object,
                _courseDbMock.Object,
                _educationPartnerDbMock.Object,
                _businessPartnerDbMock.Object,
                _userServiceMock.Object,
                _permissionServiceMock.Object,
                _blobStorageServiceMock.Object,
                _emailServiceMock.Object,
                _notificationPublisherMock.Object,
                _invoiceDbMock.Object,
                _documentLinkSettingsMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task CreateForEnrolmentIfNotExistsAsync_ReturnsExisting_WhenAlreadyExists()
        {
            var existing = new FinancialRecordModel { Id = "fr1", EnrolmentId = "e1" };
            _financialRecordDbMock.Setup(db => db.GetByEnrolmentIdAsync("e1")).ReturnsAsync(existing);

            var enrolment = new EnrolmentModel { Id = "e1" };

            var result = await _service.CreateForEnrolmentIfNotExistsAsync(enrolment, UserId);

            Assert.That(result.Id, Is.EqualTo("fr1"));
            _financialRecordDbMock.Verify(db => db.CreateAsync(It.IsAny<FinancialRecordModel>()), Times.Never);
        }

        [Test]
        public async Task CreateForEnrolmentIfNotExistsAsync_CreatesRecord_WhenNoneExists()
        {
            _financialRecordDbMock.Setup(db => db.GetByEnrolmentIdAsync("e1")).ReturnsAsync((FinancialRecordModel)null);

            var enrolment = new EnrolmentModel { Id = "e1", TuitionFee = 20000 };

            var result = await _service.CreateForEnrolmentIfNotExistsAsync(enrolment, UserId);

            Assert.That(result.EnrolmentId, Is.EqualTo("e1"));
            Assert.That(result.TotalTuition, Is.EqualTo(20000));
            _financialRecordDbMock.Verify(db => db.CreateAsync(It.IsAny<FinancialRecordModel>()), Times.Once);
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
        public void AddCommissionAdjustmentAsync_Throws_WhenRecordNotFound()
        {
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((FinancialRecordModel)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddCommissionAdjustmentAsync("missing", "Bonus", 100m, UserId));
        }

        [Test]
        public async Task AddCommissionAdjustmentAsync_AddsAdjustment_WhenFound()
        {
            var existing = new FinancialRecordModel { Id = "1", EnrolmentId = "e1" };
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);

            var result = await _service.AddCommissionAdjustmentAsync("1", "Bonus", 100m, UserId);

            Assert.That(result.Reason, Is.EqualTo("Bonus"));
            Assert.That(result.Amount, Is.EqualTo(100m));
            Assert.That(existing.ExtraCommissionAdjustments, Has.Count.EqualTo(1));
        }

        [Test]
        public void SkipPlanEntryAsync_Throws_WhenEntryAlreadyInvoiced()
        {
            var existing = new FinancialRecordModel
            {
                Id = "1",
                InvoicePlan = new List<InvoicePlanEntryModel> { new() { Id = "entry1", Status = "Invoiced" } }
            };
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SkipPlanEntryAsync("1", "entry1", "Not needed", UserId));

            Assert.That(ex!.Message, Does.Contain("already has an invoice"));
        }

        [Test]
        public void RestorePlanEntryAsync_Throws_WhenEntryNotSkipped()
        {
            var existing = new FinancialRecordModel
            {
                Id = "1",
                InvoicePlan = new List<InvoicePlanEntryModel> { new() { Id = "entry1", Status = "Planned" } }
            };
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.RestorePlanEntryAsync("1", "entry1", UserId));

            Assert.That(ex!.Message, Does.Contain("isn't skipped"));
        }

        [Test]
        public async Task AddManualPlanEntryAsync_AddsManualEntry()
        {
            var existing = new FinancialRecordModel { Id = "1", InvoicePlan = new List<InvoicePlanEntryModel>() };
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);

            var claimDate = new DateTime(2026, 6, 1);
            var result = await _service.AddManualPlanEntryAsync("1", claimDate, UserId);

            Assert.That(result.InvoicePlan, Has.Count.EqualTo(1));
            Assert.That(result.InvoicePlan[0].IsManual, Is.True);
            Assert.That(result.InvoicePlan[0].ClaimDate, Is.EqualTo(claimDate));
        }
    }
}
