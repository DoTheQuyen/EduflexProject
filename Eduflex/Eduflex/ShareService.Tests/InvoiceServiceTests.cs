using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Invoice;
using ShareService.Models.Setting;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class InvoiceServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IInvoice> _invoiceDbMock;
        private Mock<IInvoiceTemplate> _invoiceTemplateDbMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IAzureBlobDocStorageService> _blobStorageServiceMock;
        private Mock<IAzureEmailService> _emailServiceMock;
        private Mock<IInvoicePdfService> _invoicePdfServiceMock;
        private Mock<IEnrolmentService> _enrolmentServiceMock;
        private Mock<IFinancialRecord> _financialRecordDbMock;
        private Mock<IStudentPaymentPlanService> _studentPaymentPlanServiceMock;
        private Mock<ILogger<InvoiceService>> _loggerMock;
        private Mock<IOptions<DocumentLinkSettings>> _documentLinkSettingsMock;
        private InvoiceService _service;

        [SetUp]
        public void Setup()
        {
            _invoiceDbMock = new Mock<IInvoice>();
            _invoiceTemplateDbMock = new Mock<IInvoiceTemplate>();
            _userServiceMock = new Mock<IUserService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _blobStorageServiceMock = new Mock<IAzureBlobDocStorageService>();
            _emailServiceMock = new Mock<IAzureEmailService>();
            _invoicePdfServiceMock = new Mock<IInvoicePdfService>();
            _enrolmentServiceMock = new Mock<IEnrolmentService>();
            _financialRecordDbMock = new Mock<IFinancialRecord>();
            _studentPaymentPlanServiceMock = new Mock<IStudentPaymentPlanService>();
            _loggerMock = new Mock<ILogger<InvoiceService>>();
            _documentLinkSettingsMock = new Mock<IOptions<DocumentLinkSettings>>();

            _documentLinkSettingsMock.Setup(o => o.Value).Returns(new DocumentLinkSettings { ExpiryDays = 7 });

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EnrolmentsEdit.GetDescription(),
                    PermissionKey.FinanceEdit.GetDescription(),
                    PermissionKey.InvoiceTemplatesEdit.GetDescription()
                });

            _invoicePdfServiceMock.Setup(p => p.RenderToPdfAsync(It.IsAny<string>())).ReturnsAsync(new byte[] { 1, 2, 3 });
            _blobStorageServiceMock
                .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://blob/invoice.pdf");
            _blobStorageServiceMock
                .Setup(b => b.GetExpiringDownloadUri(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new Uri("https://blob/invoice.pdf?sas=1"));

            _service = new InvoiceService(
                _invoiceDbMock.Object,
                _invoiceTemplateDbMock.Object,
                _userServiceMock.Object,
                _permissionServiceMock.Object,
                _blobStorageServiceMock.Object,
                _emailServiceMock.Object,
                _invoicePdfServiceMock.Object,
                _enrolmentServiceMock.Object,
                _financialRecordDbMock.Object,
                _studentPaymentPlanServiceMock.Object,
                _loggerMock.Object,
                _documentLinkSettingsMock.Object
            );
        }

        [Test]
        public void SendInvoiceAsync_Throws_WhenCallerLacksPermission_ForStudentRecipient()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            var request = new SendInvoiceRequestModel { RecipientType = InvoiceRecipientTypes.Student, TemplateId = "t1" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.SendInvoiceAsync(request, UserId, "Staff"));
        }

        [Test]
        public void SendInvoiceAsync_Throws_WhenTemplateInactive()
        {
            _invoiceTemplateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new InvoiceTemplateModel { Id = "t1", IsActive = false });

            var request = new SendInvoiceRequestModel { RecipientType = InvoiceRecipientTypes.Student, TemplateId = "t1" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.SendInvoiceAsync(request, UserId, "Staff"));

            Assert.That(ex!.Message, Does.Contain("inactive"));
        }

        [Test]
        public async Task SendInvoiceAsync_UsesTemplateDefaultAmount_WhenStaffCannotOverride()
        {
            _invoiceTemplateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new InvoiceTemplateModel { Id = "t1", IsActive = true, DefaultAmount = 500m, DefaultGstRatePercent = 10m });
            _invoiceTemplateDbMock.Setup(db => db.ReserveNextSequenceAsync("t1")).ReturnsAsync(1);
            _invoiceDbMock.Setup(db => db.CreateAsync(It.IsAny<InvoiceModel>())).ReturnsAsync(true);

            var request = new SendInvoiceRequestModel
            {
                RecipientType = InvoiceRecipientTypes.Student,
                TemplateId = "t1",
                Amount = 999m,
                RecipientName = "Jane",
                RecipientEmail = "jane@b.com"
            };

            var result = await _service.SendInvoiceAsync(request, UserId, "Staff");

            Assert.That(result.Amount, Is.EqualTo(500m));
        }

        [Test]
        public async Task SendInvoiceAsync_AllowsManagerOverride_WhenTemplateAllowsDefault()
        {
            _invoiceTemplateDbMock.Setup(db => db.GetByIdAsync("t1"))
                .ReturnsAsync(new InvoiceTemplateModel { Id = "t1", IsActive = true, DefaultAmount = 500m, DefaultGstRatePercent = 10m });
            _invoiceTemplateDbMock.Setup(db => db.ReserveNextSequenceAsync("t1")).ReturnsAsync(1);
            _invoiceDbMock.Setup(db => db.CreateAsync(It.IsAny<InvoiceModel>())).ReturnsAsync(true);

            var request = new SendInvoiceRequestModel
            {
                RecipientType = InvoiceRecipientTypes.Student,
                TemplateId = "t1",
                Amount = 999m,
                RecipientName = "Jane",
                RecipientEmail = "jane@b.com"
            };

            var result = await _service.SendInvoiceAsync(request, UserId, "Manager");

            Assert.That(result.Amount, Is.EqualTo(999m));
        }

        [Test]
        public void ResendInvoiceAsync_Throws_WhenInvoiceCancelled()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Cancelled, RecipientType = InvoiceRecipientTypes.Student });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ResendInvoiceAsync("inv1", null, null, UserId));

            Assert.That(ex!.Message, Does.Contain("cancelled"));
        }

        [Test]
        public void ResendInvoiceAsync_Throws_WhenNoPdfOnFile()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Sent, RecipientType = InvoiceRecipientTypes.Student, PdfUrl = null });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ResendInvoiceAsync("inv1", null, null, UserId));

            Assert.That(ex!.Message, Does.Contain("no PDF"));
        }

        [Test]
        public void CancelInvoiceAsync_Throws_WhenAlreadyCancelled()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Cancelled, RecipientType = InvoiceRecipientTypes.Student });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CancelInvoiceAsync("inv1", "no longer needed", UserId));

            Assert.That(ex!.Message, Does.Contain("already cancelled"));
        }

        [Test]
        public void CancelInvoiceAsync_Throws_WhenAlreadyPaid()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Paid, RecipientType = InvoiceRecipientTypes.Student });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CancelInvoiceAsync("inv1", null, UserId));

            Assert.That(ex!.Message, Does.Contain("already marked paid"));
        }

        [Test]
        public void ConfirmPaymentAsync_Throws_WhenCancelled()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Cancelled, RecipientType = InvoiceRecipientTypes.Student });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ConfirmPaymentAsync("inv1", null, UserId));

            Assert.That(ex!.Message, Does.Contain("cancelled"));
        }

        [Test]
        public void GetDownloadLinkAsync_Throws_WhenNoPdfOnFile()
        {
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", PdfUrl = null });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetDownloadLinkAsync("inv1", UserId));

            Assert.That(ex!.Message, Does.Contain("no PDF"));
        }
    }
}
