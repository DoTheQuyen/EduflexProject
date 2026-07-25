using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Models;
using ShareService.Models.Auth;
using ShareService.Models.Setting;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserDB> _userDbMock;
        private Mock<IValidator<UpdateUserProfileModel>> _profileValidatorMock;
        private Mock<IValidator<ChangePasswordModel>> _passwordValidatorMock;
        private Mock<IValidator<UserModel>> _createUserValidatorMock;
        private Mock<ILogger<UserService>> _loggerMock;
        private Mock<IConfiguration> _configMock;
        private UserService _service;
        private Mock<IAzureEmailService> _emailService;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IOptions<WebURLSettings>> _appSettings;

        [SetUp]
        public void Setup()
        {
            _userDbMock = new Mock<IUserDB>();
            _profileValidatorMock = new Mock<IValidator<UpdateUserProfileModel>>();
            _passwordValidatorMock = new Mock<IValidator<ChangePasswordModel>>();
            _createUserValidatorMock = new Mock<IValidator<UserModel>>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _configMock = new Mock<IConfiguration>();
            _emailService = new Mock<IAzureEmailService>();
            _roleServiceMock = new Mock<IRoleService>();
            _appSettings = new Mock<IOptions<WebURLSettings>>();

            // Default validators: valid
            _profileValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserProfileModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _passwordValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ChangePasswordModel>(), default))
                .ReturnsAsync(new ValidationResult());

            // Salt for password hashing
            _configMock.Setup(c => c["JWT:Salt"]).Returns("SALT");

            // AppSettings.Value must be explicitly configured — Mock<IOptions<T>> won't
            // synthesize a T for a property getter the way it does for interface methods.
            // Without this, UserService's constructor line "_appSettings = appSettings.Value"
            // would store null, and CreateUserAsync would NullReferenceException on FrontendBaseUrl.
            _appSettings
                .Setup(a => a.Value)
                .Returns(new WebURLSettings { FrontendBaseUrl = "http://localhost:4200" });

            _service = new UserService(
                _userDbMock.Object,
                _profileValidatorMock.Object,
                _passwordValidatorMock.Object,
                _createUserValidatorMock.Object,
                _loggerMock.Object,
                _configMock.Object,
                _emailService.Object,
                _roleServiceMock.Object,
                _appSettings.Object
            );
        }

        [Test]
        public async Task GetUserByIdAsync_ReturnsUser_WhenFound()
        {
            var user = new UserModel { Id = "1", Email = "a@b.com" };
            _userDbMock.Setup(db => db.GetUserByIdAsync("1")).ReturnsAsync(user);

            var result = await _service.GetUserByIdAsync("1");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo("a@b.com"));
        }

        [Test]
        public void UpdateUserProfileAsync_Throws_WhenEmailTaken()
        {
            var existing = new UserModel { Id = "2", Email = "taken@b.com" };
            _userDbMock.Setup(db => db.GetUserByEmailAsync("taken@b.com")).ReturnsAsync(existing);

            var model = new UpdateUserProfileModel { Email = "taken@b.com" };

            Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateUserProfileAsync("1", model));
        }

        [Test]
        public async Task ChangePasswordAsync_ReturnsFalse_WhenCurrentPasswordWrong()
        {
            var user = new UserModel
            {
                Id = "1",
                PasswordHash = "wrongHash"
            };
            _userDbMock.Setup(db => db.GetUserByIdAsync("1")).ReturnsAsync(user);

            var model = new ChangePasswordModel
            {
                CurrentPassword = "current",
                NewPassword = "new"
            };

            var result = await _service.ChangePasswordAsync("1", model);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ChangePasswordAsync_ReturnsTrue_WhenPasswordUpdated()
        {
            // Arrange: hash of "currentSALT"
            var currentPassword = "current";
            var newPassword = "new";

            var currentHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(currentPassword + "SALT")));

            _userDbMock.Setup(db => db.GetUserByIdAsync("1"))
                .ReturnsAsync(new UserModel { Id = "1", PasswordHash = currentHash });

            _userDbMock.Setup(db => db.UpdatePasswordAsync("1", It.IsAny<string>()))
                .ReturnsAsync(true);

            var model = new ChangePasswordModel
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            // Act
            var result = await _service.ChangePasswordAsync("1", model);

            // Assert
            Assert.That(result, Is.True);
            _userDbMock.Verify(db => db.UpdatePasswordAsync("1", It.IsAny<string>()), Times.Once);
        }
    }
}
