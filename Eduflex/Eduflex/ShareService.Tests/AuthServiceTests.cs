using Moq;
using NUnit.Framework;
using ShareService.Models;
using ShareService.DataAccess.Interface;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ShareService.Models.Auth;

namespace ShareService.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IAuthentication> _authMock;
        private Mock<IValidator<LoginModel>> _validatorMock;
        private Mock<ILogger<AuthService>> _loggerMock;
        private AuthService _service;

        [SetUp]
        public void Setup()
        {
            _authMock = new Mock<IAuthentication>();
            _validatorMock = new Mock<IValidator<LoginModel>>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            // Default validator always returns valid
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<LoginModel>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _service = new AuthService(_authMock.Object, _validatorMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ValidateUserAsync_ReturnsUser_WhenPasswordIsCorrect()
        {
            var user = new UserModel { Email = "test@example.com", PasswordHash = "HASH" };
            _authMock.Setup(a => a.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

            var result = await _service.ValidateUserAsync(
                new LoginModel() 
                { 
                    Email = "test@example.com", 
                    Password = "password" 
                }, 
                (pwd, hash) => true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo("test@example.com"));
        }

        //[Test]
        //public async Task ValidateUserAsync_ReturnsNull_WhenUserNotFound()
        //{
        //    _authMock.Setup(a => a.FindByEmailAsync("missing@example.com")).ReturnsAsync((UserModel)null);

        //    var result = await _service.ValidateUserAsync("missing@example.com", "pwd", (pwd, hash) => true);

        //    Assert.That(result, Is.Not.Null);
        //}

        [Test]
        public async Task UpdateLastLoginAsync_CallsRepository()
        {
            await _service.UpdateLastLoginAsync("userid123");
            _authMock.Verify(a => a.UpdateLastLoginAsync("userid123"), Times.Once);
        }
    }
}
