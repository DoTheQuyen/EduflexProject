using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Accounts;
using ShareService.Models.BusinessPartner;
using ShareService.Models.EducationPartner;
using ShareService.Models.Enrolment;
using ShareService.Models.Invoice;
using ShareService.Models.StudentPaymentPlan;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class AccountsServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IStudentPaymentPlanEntry> _studentPlanDbMock;
        private Mock<IFinancialRecord> _financialRecordDbMock;
        private Mock<IInvoice> _invoiceDbMock;
        private Mock<IEnrolment> _enrolmentDbMock;
        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<IBusinessPartner> _businessPartnerDbMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private AccountsService _service;

        [SetUp]
        public void Setup()
        {
            _studentPlanDbMock = new Mock<IStudentPaymentPlanEntry>();
            _financialRecordDbMock = new Mock<IFinancialRecord>();
            _invoiceDbMock = new Mock<IInvoice>();
            _enrolmentDbMock = new Mock<IEnrolment>();
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _businessPartnerDbMock = new Mock<IBusinessPartner>();
            _permissionServiceMock = new Mock<IPermissionService>();

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.FinanceView.GetDescription() });

            _studentPlanDbMock.Setup(db => db.GetDueByAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<StudentPaymentPlanEntryModel>());
            _studentPlanDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<StudentPaymentPlanEntryModel>());
            _financialRecordDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<ShareService.Models.Financial.FinancialRecordModel>());

            _service = new AccountsService(
                _studentPlanDbMock.Object,
                _financialRecordDbMock.Object,
                _invoiceDbMock.Object,
                _enrolmentDbMock.Object,
                _educationPartnerDbMock.Object,
                _businessPartnerDbMock.Object,
                _permissionServiceMock.Object
            );
        }

        [Test]
        public void GetActionQueueAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetActionQueueAsync(14, UserId));
        }

        [Test]
        public async Task GetActionQueueAsync_IncludesOverdueStudentEntry()
        {
            var overdueEntry = new StudentPaymentPlanEntryModel
            {
                Id = "entry1",
                EnrolmentId = "e1",
                StudentName = "Jane Doe",
                Status = "Planned",
                DueDate = DateTime.UtcNow.Date.AddDays(-5),
                Amount = 500m
            };
            _studentPlanDbMock.Setup(db => db.GetDueByAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<StudentPaymentPlanEntryModel> { overdueEntry });

            var result = await _service.GetActionQueueAsync(14, UserId);

            Assert.That(result.OverdueCount, Is.EqualTo(1));
            Assert.That(result.OverdueAmount, Is.EqualTo(500m));
            Assert.That(result.Items[0].Reason, Is.EqualTo(ActionQueueReasons.Overdue));
        }

        [Test]
        public async Task GetActionQueueAsync_ExcludesEntry_WhenInvoicedAndPaid()
        {
            var paidEntry = new StudentPaymentPlanEntryModel
            {
                Id = "entry1",
                EnrolmentId = "e1",
                StudentName = "Jane Doe",
                Status = "Invoiced",
                DueDate = DateTime.UtcNow.Date.AddDays(-5),
                LinkedInvoiceId = "inv1",
                Amount = 500m
            };
            _studentPlanDbMock.Setup(db => db.GetDueByAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<StudentPaymentPlanEntryModel> { paidEntry });
            _invoiceDbMock.Setup(db => db.GetByIdAsync("inv1"))
                .ReturnsAsync(new InvoiceModel { Id = "inv1", Status = InvoiceStatuses.Paid, Total = 500m });

            var result = await _service.GetActionQueueAsync(14, UserId);

            Assert.That(result.Items, Is.Empty);
        }

        [Test]
        public async Task GetPortfolioAsync_FiltersBySearchTerm()
        {
            _studentPlanDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<StudentPaymentPlanEntryModel>
            {
                new() { EnrolmentId = "e1", StudentName = "Jane Doe", FeeType = "Tuition", Amount = 100m, Status = "Planned", DueDate = DateTime.UtcNow.AddDays(10) },
                new() { EnrolmentId = "e2", StudentName = "John Smith", FeeType = "Tuition", Amount = 100m, Status = "Planned", DueDate = DateTime.UtcNow.AddDays(10) }
            });
            _invoiceDbMock.Setup(db => db.GetByEnrolmentIdAsync(It.IsAny<string>())).ReturnsAsync(new List<InvoiceModel>());

            var result = await _service.GetPortfolioAsync("Jane", null, null, 1, 20, UserId);

            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Name, Is.EqualTo("Jane Doe"));
        }

        [Test]
        public async Task GetPortfolioAsync_DerivesOverdueStatus_WhenNextDueInPast()
        {
            _studentPlanDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<StudentPaymentPlanEntryModel>
            {
                new() { EnrolmentId = "e1", StudentName = "Jane Doe", FeeType = "Tuition", Amount = 100m, Status = "Planned", DueDate = DateTime.UtcNow.AddDays(-10) }
            });
            _invoiceDbMock.Setup(db => db.GetByEnrolmentIdAsync(It.IsAny<string>())).ReturnsAsync(new List<InvoiceModel>());

            var result = await _service.GetPortfolioAsync(null, AccountTypes.Student, null, 1, 20, UserId);

            Assert.That(result.Items[0].Status, Is.EqualTo(AccountStatuses.Overdue));
        }

        [Test]
        public void GetAccountTimelineAsync_Throws_WhenNoStudentPlanFound()
        {
            _studentPlanDbMock.Setup(db => db.GetByEnrolmentIdAsync("e1")).ReturnsAsync(new List<StudentPaymentPlanEntryModel>());

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetAccountTimelineAsync(AccountTypes.Student, "e1", UserId));
        }

        [Test]
        public void GetAccountTimelineAsync_Throws_WhenFinancialRecordNotFound()
        {
            _financialRecordDbMock.Setup(db => db.GetByIdAsync("fr1")).ReturnsAsync((ShareService.Models.Financial.FinancialRecordModel)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetAccountTimelineAsync(AccountTypes.EducationPartner, "fr1", UserId));
        }

        [Test]
        public async Task GetAccountTimelineAsync_ReturnsStudentTimeline_WithComputedTotals()
        {
            _studentPlanDbMock.Setup(db => db.GetByEnrolmentIdAsync("e1")).ReturnsAsync(new List<StudentPaymentPlanEntryModel>
            {
                new() { Id = "entry1", EnrolmentId = "e1", StudentName = "Jane Doe", CourseName = "IT Degree", FeeType = "Tuition", Amount = 300m, Status = "Planned", DueDate = DateTime.UtcNow.AddDays(10) },
                new() { Id = "entry2", EnrolmentId = "e1", StudentName = "Jane Doe", CourseName = "IT Degree", FeeType = "Tuition", Amount = 300m, Status = "Invoiced", LinkedInvoiceId = "inv1", DueDate = DateTime.UtcNow.AddDays(-5) }
            });
            _invoiceDbMock.Setup(db => db.GetByEnrolmentIdAsync("e1")).ReturnsAsync(new List<InvoiceModel>
            {
                new() { Id = "inv1", Status = InvoiceStatuses.Paid, Total = 300m, InvoiceNo = "INV-0001" }
            });

            var result = await _service.GetAccountTimelineAsync(AccountTypes.Student, "e1", UserId);

            Assert.That(result.ContractTotal, Is.EqualTo(600m));
            Assert.That(result.Received, Is.EqualTo(300m));
            Assert.That(result.Outstanding, Is.EqualTo(300m));
            Assert.That(result.Entries, Has.Count.EqualTo(2));
        }
    }
}
