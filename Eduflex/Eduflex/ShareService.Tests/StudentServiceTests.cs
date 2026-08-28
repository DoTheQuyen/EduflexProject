using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Auth;
using ShareService.Models.Setting;
using ShareService.Models.Student;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class StudentServiceTests
    {
        private const string ActingUserId = "staff-1";

        private Mock<IApplication> _applicationDbMock;
        private Mock<IStudentDB> _studentDbMock;
        private Mock<IUserDB> _userDbMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IAzureEmailService> _emailServiceMock;
        private Mock<IEnrolment> _enrolmentDbMock;
        private Mock<IAzureBlobDocStorageService> _blobStorageServiceMock;
        private Mock<IValidator<StudentModel>> _validatorMock;
        private Mock<IOptions<WebURLSettings>> _appSettingsMock;
        private Mock<ILogger<StudentService>> _loggerMock;
        private StudentService _service;

        [SetUp]
        public void Setup()
        {
            _applicationDbMock = new Mock<IApplication>();
            _studentDbMock = new Mock<IStudentDB>();
            _userDbMock = new Mock<IUserDB>();
            _userServiceMock = new Mock<IUserService>();
            _roleServiceMock = new Mock<IRoleService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _emailServiceMock = new Mock<IAzureEmailService>();
            _enrolmentDbMock = new Mock<IEnrolment>();
            _blobStorageServiceMock = new Mock<IAzureBlobDocStorageService>();
            _validatorMock = new Mock<IValidator<StudentModel>>();
            _appSettingsMock = new Mock<IOptions<WebURLSettings>>();
            _loggerMock = new Mock<ILogger<StudentService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<StudentModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.StudentsView.GetDescription(),
                    PermissionKey.StudentsAdd.GetDescription(),
                    PermissionKey.StudentsEdit.GetDescription(),
                    PermissionKey.StudentsDelete.GetDescription()
                });

            _appSettingsMock
                .Setup(a => a.Value)
                .Returns(new WebURLSettings { FrontendBaseUrl = "http://localhost:4200" });

            // No user matches by default — CheckDuplicateAsync/CreateStudentAsync see "no duplicate".
            _userDbMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserModel)null);
            _userDbMock.Setup(db => db.GetUserByMobileAsync(It.IsAny<string>())).ReturnsAsync((UserModel)null);
            _studentDbMock.Setup(db => db.GetByPassportNumberAsync(It.IsAny<string>())).ReturnsAsync((StudentModel)null);
            _studentDbMock.Setup(db => db.GetByDateOfBirthAsync(It.IsAny<DateTime>())).ReturnsAsync((StudentModel)null);

            _service = new StudentService(
                _applicationDbMock.Object,
                _studentDbMock.Object,
                _userDbMock.Object,
                _userServiceMock.Object,
                _roleServiceMock.Object,
                _permissionServiceMock.Object,
                _emailServiceMock.Object,
                _enrolmentDbMock.Object,
                _blobStorageServiceMock.Object,
                _validatorMock.Object,
                _appSettingsMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public void GetMyProfileAsync_Throws_WhenUserIdEmpty()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetMyProfileAsync(""));
        }

        [Test]
        public async Task GetMyProfileAsync_ReturnsProfile_WhenFound()
        {
            _applicationDbMock
                .Setup(db => db.GetStudentByUserIdAsync("user-1"))
                .ReturnsAsync(new StudentModel { Id = "s1", UserId = "user-1" });

            var result = await _service.GetMyProfileAsync("user-1");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("s1"));
        }

        [Test]
        public void SearchStudentsAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(ActingUserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.SearchStudentsAsync(new StudentFilter(), ActingUserId));
        }

        [Test]
        public async Task CheckDuplicateAsync_ReturnsDuplicate_WhenEmailMatchesActiveUser()
        {
            _userDbMock
                .Setup(db => db.GetUserByEmailAsync("taken@b.com"))
                .ReturnsAsync(new UserModel { Id = "user-2", Email = "taken@b.com", IsActive = true });
            _studentDbMock
                .Setup(db => db.GetByUserIdAsync("user-2"))
                .ReturnsAsync(new StudentModel { Id = "s2", UserId = "user-2" });

            var result = await _service.CheckDuplicateAsync("taken@b.com", "999", DateTime.UtcNow.AddYears(-20), "P123", ActingUserId);

            Assert.That(result.IsDuplicate, Is.True);
            Assert.That(result.MatchedField, Is.EqualTo("Email"));
        }

        [Test]
        public async Task CheckDuplicateAsync_ReturnsNotDuplicate_WhenNoMatch()
        {
            var result = await _service.CheckDuplicateAsync("new@b.com", "999", DateTime.UtcNow.AddYears(-20), "P999", ActingUserId);

            Assert.That(result.IsDuplicate, Is.False);
        }

        [Test]
        public void CreateStudentAsync_Throws_WhenDuplicateFound()
        {
            _userDbMock
                .Setup(db => db.GetUserByEmailAsync("taken@b.com"))
                .ReturnsAsync(new UserModel { Id = "user-2", Email = "taken@b.com", IsActive = true });
            _studentDbMock
                .Setup(db => db.GetByUserIdAsync("user-2"))
                .ReturnsAsync((StudentModel)null);

            var newUser = new UserModel { Email = "taken@b.com", Mobile = "999" };
            var profile = new StudentModel { DateOfBirth = DateTime.UtcNow.AddYears(-20), PassportNumber = "P123" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateStudentAsync(newUser, profile, ActingUserId));

            Assert.That(ex!.Message, Does.Contain("already exists"));
        }

        [Test]
        public void CreateStudentAsync_Throws_WhenStudentRoleNotConfigured()
        {
            _roleServiceMock
                .Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(new List<ShareService.Models.Role.RoleModel>());

            var newUser = new UserModel { Email = "new@b.com", Mobile = "999" };
            var profile = new StudentModel { DateOfBirth = DateTime.UtcNow.AddYears(-20), PassportNumber = "P999" };

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateStudentAsync(newUser, profile, ActingUserId));
        }

        [Test]
        public void UpdateStudentAsync_Throws_WhenEmailEmpty()
        {
            var profile = new StudentModel();

            Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateStudentAsync("1", "", "999", profile, ActingUserId));
        }

        [Test]
        public void DeactivateStudentAsync_Throws_WhenStudentNotFound()
        {
            _studentDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((StudentModel)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeactivateStudentAsync("missing", ActingUserId));
        }

        [Test]
        public async Task DeactivateStudentAsync_DeactivatesUser_WhenFound()
        {
            var student = new StudentModel { Id = "1", UserId = "user-1" };
            _studentDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(student);
            _enrolmentDbMock
                .Setup(db => db.GetByStudentUserIdAsync("user-1"))
                .ReturnsAsync(new List<ShareService.Models.Enrolment.EnrolmentModel>());
            _userDbMock.Setup(db => db.SetActiveStatusAsync("user-1", false)).ReturnsAsync(true);

            var result = await _service.DeactivateStudentAsync("1", ActingUserId);

            Assert.That(result, Is.True);
        }
    }
}
