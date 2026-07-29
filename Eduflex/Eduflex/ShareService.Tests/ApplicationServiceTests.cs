using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;
using ShareService.Models.Application;

namespace ShareService.Tests
{
    [TestFixture]
    public class ApplicationServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IApplication> _appRepoMock;
        private Mock<IValidator<ApplicationModel>> _validatorMock;
        private Mock<ILogger<ApplicationService>> _loggerMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private ApplicationService _service;

        [SetUp]
        public void Setup()
        {
            _appRepoMock = new Mock<IApplication>();
            _validatorMock = new Mock<IValidator<ApplicationModel>>();
            _loggerMock = new Mock<ILogger<ApplicationService>>();
            _permissionServiceMock = new Mock<IPermissionService>();

            // Validator always passes
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ApplicationModel>(), default))
                .ReturnsAsync(new ValidationResult());

            // Caller has the ApplicationsAdd permission by default
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.ApplicationsAdd.GetDescription() });

            // Default mock for CreateApplicationAsync returns the same app
            _appRepoMock
                .Setup(r => r.CreateApplicationAsync(It.IsAny<ApplicationModel>(), null))
                .ReturnsAsync((ApplicationModel app, MongoDB.Driver.IClientSessionHandle? _) =>
                {
                    app.Id = "mock-id"; // simulate DB-generated Id
                    return app;
                });

            _service = new ApplicationService(
                _appRepoMock.Object,
                _validatorMock.Object,
                _loggerMock.Object,
                _permissionServiceMock.Object
            );
        }

        [Test]
        public async Task CreateApplication_SetsDefaultStatus()
        {
            // Arrange
            var createDto = new ApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Test"
            };

            // Act
            var result = await _service.CreateApplication(createDto, UserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Pending"));
            Assert.That(result.Id, Is.EqualTo("mock-id"));
        }

        [Test]
        public void CreateApplication_Throws_WhenValidationFails()
        {
            // Arrange: force validator to fail
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ApplicationModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("StudentId", "StudentId is required")
                }));

            var createDto = new ApplicationModel
            {
                StudentId = "",
                StudentName = "John"
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateApplication(createDto, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void CreateApplication_Throws_WhenCallerLacksPermission()
        {
            // Arrange: caller has no permissions at all
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            var createDto = new ApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Test"
            };

            // Act + Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.CreateApplication(createDto, UserId));
        }

        [Test]
        public void CreateApplication_PropagatesException_OnRepoFailure()
        {
            // Arrange: repo throws exception
            _appRepoMock
                .Setup(r => r.CreateApplicationAsync(It.IsAny<ApplicationModel>(), null))
                .ThrowsAsync(new Exception("DB error"));

            var createDto = new ApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Should fail"
            };

            // Act + Assert
            Assert.ThrowsAsync<Exception>(async () => await _service.CreateApplication(createDto, UserId));
        }
    }
}
