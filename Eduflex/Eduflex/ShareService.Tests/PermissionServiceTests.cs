using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;
using ShareService.Models.Permission;
using ShareService.Models.Role;
using ShareService.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class PermissionServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IUserDB> _userDbMock;
        private Mock<IRole> _roleDbMock;
        private Mock<IPermissionCatalog> _permissionCatalogMock;
        private Mock<IMemoryCache> _cacheMock;
        private Mock<ILogger<PermissionService>> _loggerMock;
        private PermissionService _service;

        [SetUp]
        public void Setup()
        {
            _userDbMock = new Mock<IUserDB>();
            _roleDbMock = new Mock<IRole>();
            _permissionCatalogMock = new Mock<IPermissionCatalog>();
            _cacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<PermissionService>>();

            // Cache miss by default.
            object cached = null;
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cached)).Returns(false);

            var cacheEntryMock = new Mock<ICacheEntry>();
            cacheEntryMock.SetupAllProperties();
            _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            _service = new PermissionService(
                _userDbMock.Object,
                _roleDbMock.Object,
                _permissionCatalogMock.Object,
                _cacheMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsPermissionKeys_ForActiveUserWithRole()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserModel { Id = UserId, RoleId = "role-1", IsActive = true });
            _roleDbMock.Setup(db => db.GetByIdAsync("role-1"))
                .ReturnsAsync(new RoleModel { Id = "role-1", PermissionIds = new List<string> { "p1", "p2" } });
            _permissionCatalogMock.Setup(db => db.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<PermissionModel>
                {
                    new() { Id = "p1", Key = "Users.View" },
                    new() { Id = "p2", Key = "Users.Edit" }
                });

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.EquivalentTo(new[] { "Users.View", "Users.Edit" }));
            _cacheMock.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsCached_WithoutHittingDb()
        {
            object cached = new List<string> { "Users.View" };
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cached)).Returns(true);

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.EquivalentTo(new[] { "Users.View" }));
            _userDbMock.Verify(db => db.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsEmptyList_WhenUserNotFound()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId)).ReturnsAsync((UserModel)null);

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsEmptyList_WhenUserInactive()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserModel { Id = UserId, IsActive = false });

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsEmptyList_WhenRoleNotFound()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserModel { Id = UserId, RoleId = "missing-role", IsActive = true });
            _roleDbMock.Setup(db => db.GetByIdAsync("missing-role")).ReturnsAsync((RoleModel)null);

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsEmptyList_WhenRoleHasNoPermissions()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserModel { Id = UserId, RoleId = "role-1", IsActive = true });
            _roleDbMock.Setup(db => db.GetByIdAsync("role-1"))
                .ReturnsAsync(new RoleModel { Id = "role-1", PermissionIds = new List<string>() });

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPermissionsForUserAsync_ReturnsEmptyList_WhenDataAccessThrows()
        {
            _userDbMock.Setup(db => db.GetUserByIdAsync(UserId)).ThrowsAsync(new Exception("DB error"));

            var result = await _service.GetPermissionsForUserAsync(UserId);

            Assert.That(result, Is.Empty);
        }
    }
}
