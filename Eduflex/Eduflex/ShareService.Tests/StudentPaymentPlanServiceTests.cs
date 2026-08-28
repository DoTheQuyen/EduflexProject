using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Auth;
using ShareService.Models.StudentPaymentPlan;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class StudentPaymentPlanServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IStudentPaymentPlanEntry> _dataAccessMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IUserService> _userServiceMock;
        private StudentPaymentPlanService _service;

        [SetUp]
        public void Setup()
        {
            _dataAccessMock = new Mock<IStudentPaymentPlanEntry>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _userServiceMock = new Mock<IUserService>();

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EnrolmentsView.GetDescription(),
                    PermissionKey.EnrolmentsEdit.GetDescription()
                });

            _userServiceMock
                .Setup(u => u.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserModel { Id = UserId, FirstName = "Jane", LastName = "Staff" });

            _dataAccessMock
                .Setup(db => db.GetByEnrolmentIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<StudentPaymentPlanEntryModel>());

            _service = new StudentPaymentPlanService(_dataAccessMock.Object, _permissionServiceMock.Object, _userServiceMock.Object);
        }

        [Test]
        public void GetByEnrolmentIdAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByEnrolmentIdAsync("e1", UserId));
        }

        [Test]
        public void GeneratePlanAsync_Throws_WhenInstalmentCountLessThanOne()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GeneratePlanAsync("e1", "Jane Doe", "IT Degree", "Tuition", 3000m, 0, DateTime.UtcNow, 1, UserId));

            Assert.That(ex!.Message, Does.Contain("at least one instalment"));
        }

        [Test]
        public void GeneratePlanAsync_Throws_WhenPlanAlreadyExistsForFeeType()
        {
            _dataAccessMock
                .Setup(db => db.GetByEnrolmentIdAsync("e1"))
                .ReturnsAsync(new List<StudentPaymentPlanEntryModel> { new() { FeeType = "Tuition" } });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GeneratePlanAsync("e1", "Jane Doe", "IT Degree", "Tuition", 3000m, 3, DateTime.UtcNow, 1, UserId));

            Assert.That(ex!.Message, Does.Contain("already has a Tuition payment plan"));
        }

        [Test]
        public async Task GeneratePlanAsync_SplitsAmountEvenly_WithRemainderOnLastInstalment()
        {
            var firstDue = new DateTime(2026, 1, 1);

            var result = await _service.GeneratePlanAsync("e1", "Jane Doe", "IT Degree", "Tuition", 1000m, 3, firstDue, 1, UserId);

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0].Amount, Is.EqualTo(333.33m));
            Assert.That(result[1].Amount, Is.EqualTo(333.33m));
            Assert.That(result[2].Amount, Is.EqualTo(333.34m));
            Assert.That(result.Sum(e => e.Amount), Is.EqualTo(1000m));
            Assert.That(result[1].DueDate, Is.EqualTo(firstDue.AddMonths(1)));
        }

        [Test]
        public async Task AddManualEntryAsync_NumbersEntrySequentially_AfterExistingOfSameFeeType()
        {
            _dataAccessMock
                .Setup(db => db.GetByEnrolmentIdAsync("e1"))
                .ReturnsAsync(new List<StudentPaymentPlanEntryModel>
                {
                    new() { FeeType = "Tuition" },
                    new() { FeeType = "Tuition" }
                });

            var result = await _service.AddManualEntryAsync("e1", "Jane Doe", "IT Degree", "Tuition", "Late fee", 200m, DateTime.UtcNow, UserId);

            Assert.That(result.InstalmentNumber, Is.EqualTo(3));
            Assert.That(result.IsManual, Is.True);
        }

        [Test]
        public void UpdateEntryDateAsync_Throws_WhenEntryNotPlanned()
        {
            _dataAccessMock.Setup(db => db.GetByIdAsync("entry1"))
                .ReturnsAsync(new StudentPaymentPlanEntryModel { Id = "entry1", Status = StudentPaymentPlanEntryStatuses.Invoiced });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateEntryDateAsync("entry1", DateTime.UtcNow, UserId));

            Assert.That(ex!.Message, Does.Contain("Only a Planned instalment"));
        }

        [Test]
        public void SkipEntryAsync_Throws_WhenEntryNotPlanned()
        {
            _dataAccessMock.Setup(db => db.GetByIdAsync("entry1"))
                .ReturnsAsync(new StudentPaymentPlanEntryModel { Id = "entry1", Status = StudentPaymentPlanEntryStatuses.Skipped });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SkipEntryAsync("entry1", "not needed", UserId));

            Assert.That(ex!.Message, Does.Contain("Only a Planned instalment"));
        }

        [Test]
        public void RestoreEntryAsync_Throws_WhenEntryNotSkipped()
        {
            _dataAccessMock.Setup(db => db.GetByIdAsync("entry1"))
                .ReturnsAsync(new StudentPaymentPlanEntryModel { Id = "entry1", Status = StudentPaymentPlanEntryStatuses.Planned });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.RestoreEntryAsync("entry1", UserId));

            Assert.That(ex!.Message, Does.Contain("Only a Skipped instalment"));
        }

        [Test]
        public async Task MarkEntryInvoicedAsync_NoOps_WhenEntryNotFound()
        {
            _dataAccessMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((StudentPaymentPlanEntryModel)null);

            await _service.MarkEntryInvoicedAsync("missing", "inv1");

            _dataAccessMock.Verify(db => db.ReplaceAsync(It.IsAny<string>(), It.IsAny<StudentPaymentPlanEntryModel>()), Times.Never);
        }
    }
}
