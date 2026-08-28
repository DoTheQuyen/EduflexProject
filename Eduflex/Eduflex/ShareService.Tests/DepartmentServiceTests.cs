using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Auth;
using ShareService.Models.Department;
using ShareService.Models.Role;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class DepartmentServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IDepartment> _departmentDbMock;
        private Mock<IUserDB> _userDbMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IValidator<DepartmentModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<DepartmentService>> _loggerMock;
        private DepartmentService _service;

        [SetUp]
        public void Setup()
        {
            _departmentDbMock = new Mock<IDepartment>();
            _userDbMock = new Mock<IUserDB>();
            _roleServiceMock = new Mock<IRoleService>();
            _validatorMock = new Mock<IValidator<DepartmentModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<DepartmentService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DepartmentModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.DepartmentsView.GetDescription(),
                    PermissionKey.DepartmentsAdd.GetDescription(),
                    PermissionKey.DepartmentsEdit.GetDescription(),
                    PermissionKey.DepartmentsDelete.GetDescription()
                });

            _departmentDbMock.Setup(db => db.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DepartmentModel)null);
            _roleServiceMock.Setup(r => r.GetAllRolesAsync()).ReturnsAsync(new List<RoleModel>());

            _service = new DepartmentService(
                _departmentDbMock.Object,
                _userDbMock.Object,
                _roleServiceMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetAllDepartmentsAsync_ReturnsDepartments_NoPermissionRequired()
        {
            _departmentDbMock.Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<DepartmentModel> { new() { Id = "1", Name = "IT" } });

            var result = await _service.GetAllDepartmentsAsync();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetDepartmentsManagedByUserAsync_FiltersByHeadUserId()
        {
            _departmentDbMock.Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<DepartmentModel>
                {
                    new() { Id = "1", Name = "IT", HeadUserId = UserId },
                    new() { Id = "2", Name = "Finance", HeadUserId = "someone-else" }
                });

            var result = await _service.GetDepartmentsManagedByUserAsync(UserId);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo("1"));
        }

        [Test]
        public void GetDepartmentByIdAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetDepartmentByIdAsync("1", UserId));
        }

        [Test]
        public async Task CreateDepartmentAsync_ReturnsTrue_WhenCreated()
        {
            _departmentDbMock.Setup(db => db.CreateDepartmentAsync(It.IsAny<DepartmentModel>())).ReturnsAsync(true);

            var department = new DepartmentModel { Name = "IT" };

            var result = await _service.CreateDepartmentAsync(department, UserId);

            Assert.That(result, Is.True);
            Assert.That(department.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateDepartmentAsync_Throws_WhenNameAlreadyExists()
        {
            _departmentDbMock.Setup(db => db.GetByNameAsync("IT"))
                .ReturnsAsync(new DepartmentModel { Id = "1", Name = "IT" });

            var department = new DepartmentModel { Name = "IT" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDepartmentAsync(department, UserId));

            Assert.That(ex!.Message, Does.Contain("already exists"));
        }

        [Test]
        public void CreateDepartmentAsync_Throws_WhenParentDepartmentNotFound()
        {
            _departmentDbMock.Setup(db => db.GetDepartmentByIdAsync("missing-parent")).ReturnsAsync((DepartmentModel)null);

            var department = new DepartmentModel { Name = "IT", ParentDepartmentId = "missing-parent" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDepartmentAsync(department, UserId));

            Assert.That(ex!.Message, Does.Contain("Parent department not found"));
        }

        [Test]
        public void CreateDepartmentAsync_Throws_WhenMemberIsStudent()
        {
            _userDbMock.Setup(db => db.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<UserModel> { new() { Id = "student-1", RoleId = "role-student" } });
            _roleServiceMock.Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(new List<RoleModel> { new() { Id = "role-student", RoleType = RoleTypeEnums.Student } });

            var department = new DepartmentModel { Name = "IT", MemberUserIds = new List<string> { "student-1" } };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDepartmentAsync(department, UserId));

            Assert.That(ex!.Message, Does.Contain("Students cannot be assigned"));
        }

        [Test]
        public void UpdateDepartmentAsync_Throws_WhenNotFound()
        {
            _departmentDbMock.Setup(db => db.GetDepartmentByIdAsync("missing")).ReturnsAsync((DepartmentModel)null);

            var department = new DepartmentModel { Name = "IT" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDepartmentAsync("missing", department, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public void DeleteDepartmentAsync_Throws_WhenChildDepartmentsExist()
        {
            _departmentDbMock.Setup(db => db.HasChildDepartmentsAsync("1")).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteDepartmentAsync("1", UserId));

            Assert.That(ex!.Message, Does.Contain("nested under"));
            _departmentDbMock.Verify(db => db.DeleteDepartmentAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task DeleteDepartmentAsync_ReturnsTrue_WhenDeleted()
        {
            _departmentDbMock.Setup(db => db.HasChildDepartmentsAsync("1")).ReturnsAsync(false);
            _departmentDbMock.Setup(db => db.DeleteDepartmentAsync("1")).ReturnsAsync(true);

            var result = await _service.DeleteDepartmentAsync("1", UserId);

            Assert.That(result, Is.True);
        }
    }
}
