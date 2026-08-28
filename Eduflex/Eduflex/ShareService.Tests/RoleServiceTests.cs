using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Permission;
using ShareService.Models.Role;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class RoleServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IRole> _roleDbMock;
        private Mock<IPermissionCatalog> _permissionCatalogMock;
        private Mock<IValidator<RoleModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<IUserDB> _userDbMock;
        private Mock<ILogger<RoleService>> _loggerMock;
        private RoleService _service;

        [SetUp]
        public void Setup()
        {
            _roleDbMock = new Mock<IRole>();
            _permissionCatalogMock = new Mock<IPermissionCatalog>();
            _validatorMock = new Mock<IValidator<RoleModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _userDbMock = new Mock<IUserDB>();
            _loggerMock = new Mock<ILogger<RoleService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<RoleModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.RolesView.GetDescription(), PermissionKey.RolesAdd.GetDescription() });

            _roleDbMock.Setup(db => db.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((RoleModel)null);

            _service = new RoleService(
                _roleDbMock.Object,
                _permissionCatalogMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _userDbMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetPermissionsAsync_ReturnsPermissionKeys_WhenRoleHasPermissions()
        {
            _roleDbMock.Setup(db => db.GetByIdAsync("role-1"))
                .ReturnsAsync(new RoleModel { Id = "role-1", PermissionIds = new List<string> { "p1" } });
            _permissionCatalogMock.Setup(pc => pc.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<PermissionModel> { new() { Id = "p1", Key = "Users.View" } });

            var result = await _service.GetPermissionsAsync("role-1");

            Assert.That(result, Is.EquivalentTo(new[] { "Users.View" }));
        }

        [Test]
        public async Task GetPermissionsAsync_ReturnsEmptyList_WhenRoleNotFound()
        {
            _roleDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((RoleModel)null);

            var result = await _service.GetPermissionsAsync("missing");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetRolesAsync_AttachesUserCounts()
        {
            _roleDbMock.Setup(db => db.GetRolesAsync(It.IsAny<PaginationQuery>()))
                .ReturnsAsync(new PagedResult<RoleModel>
                {
                    Items = new List<RoleModel> { new() { Id = "role-1", Name = "Staff" } }
                });
            _userDbMock.Setup(db => db.CountUsersByRoleIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, int> { { "role-1", 5 } });

            var result = await _service.GetRolesAsync(new PaginationQuery(), UserId);

            Assert.That(result.Items[0].UserCount, Is.EqualTo(5));
        }

        [Test]
        public void GetRolesAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetRolesAsync(new PaginationQuery(), UserId));
        }

        [Test]
        public async Task CreateRoleAsync_ReturnsTrue_WhenCreated()
        {
            _roleDbMock.Setup(db => db.CreateAsync(It.IsAny<RoleModel>())).ReturnsAsync(true);

            var role = new RoleModel { Name = "Manager" };

            var result = await _service.CreateRoleAsync(role, UserId);

            Assert.That(result, Is.True);
            Assert.That(role.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateRoleAsync_Throws_WhenNameAlreadyExists()
        {
            _roleDbMock.Setup(db => db.GetByNameAsync("Manager"))
                .ReturnsAsync(new RoleModel { Id = "1", Name = "Manager" });

            var role = new RoleModel { Name = "Manager" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRoleAsync(role, UserId));

            Assert.That(ex!.Message, Does.Contain("already exists"));
        }

        [Test]
        public void UpdateRoleAsync_Throws_WhenNotFound()
        {
            _roleDbMock.Setup(db => db.GetByIdAsync("missing")).ReturnsAsync((RoleModel)null);

            var role = new RoleModel { Name = "Manager" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateRoleAsync("missing", role, UserId));

            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public void UpdateRoleAsync_Throws_WhenRenamedToExistingRole()
        {
            _roleDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(new RoleModel { Id = "1", Name = "Staff" });
            _roleDbMock.Setup(db => db.GetByNameAsync("Manager"))
                .ReturnsAsync(new RoleModel { Id = "2", Name = "Manager" });

            var role = new RoleModel { Name = "Manager" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateRoleAsync("1", role, UserId));

            Assert.That(ex!.Message, Does.Contain("already exists"));
        }
    }
}
