using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Services;
using ShareService.Services.Interface;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using FluentValidation.Results;
using ShareService.Models.Application;

namespace ShareService.Tests
{
    [TestFixture]
    public class ApplicationServiceTests
    {
        private Mock<IApplication> _appRepoMock;
        private Mock<IValidator<CreateApplicationModel>> _validatorMock;
        private Mock<ILogger<ApplicationService>> _loggerMock;
        private Mock<IMongoClient> _clientMock;
        private Mock<IClientSessionHandle> _sessionMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private ApplicationService _service;

        [SetUp]
        public void Setup()
        {
            _appRepoMock = new Mock<IApplication>();
            _validatorMock = new Mock<IValidator<CreateApplicationModel>>();
            _loggerMock = new Mock<ILogger<ApplicationService>>();
            _clientMock = new Mock<IMongoClient>();
            _sessionMock = new Mock<IClientSessionHandle>();
            _permissionServiceMock = new Mock<IPermissionService>();

            // Validator always passes
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CreateApplicationModel>(), default))
                .ReturnsAsync(new ValidationResult());

            // Mongo client returns a fake session
            _clientMock
                .Setup(c => c.StartSessionAsync(
                    It.IsAny<ClientSessionOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_sessionMock.Object);

            // Default mock for CreateApplicationAsync returns the same app
            _appRepoMock
                .Setup(r => r.CreateApplicationAsync(
                    It.IsAny<ApplicationModel>(),
                    It.IsAny<IClientSessionHandle?>()))
                .ReturnsAsync((ApplicationModel app, IClientSessionHandle? _) =>
                {
                    app.Id = "mock-id"; // simulate DB-generated Id
                    return app;
                });

            _service = new ApplicationService(
                _appRepoMock.Object,
                _validatorMock.Object,
                _loggerMock.Object,
                _clientMock.Object,
                _permissionServiceMock.Object
            );
        }

        [Test]
        public async Task CreateApplication_SetsDefaultStatus()
        {
            // Arrange
            var createDto = new CreateApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Test"
            };

            // Act
            var result = await _service.CreateApplication(createDto);

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
                .Setup(v => v.ValidateAsync(It.IsAny<CreateApplicationModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("StudentId", "StudentId is required")
                }));

            var createDto = new CreateApplicationModel
            {
                StudentId = "",
                StudentName = "John"
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateApplication(createDto));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public async Task CreateApplication_CommitsTransaction()
        {
            // Arrange
            var createDto = new CreateApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Transaction test"
            };

            // Act
            var result = await _service.CreateApplication(createDto);

            // Assert
            _sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CreateApplication_AbortsTransaction_OnException()
        {
            // Arrange: repo throws exception
            _appRepoMock
                .Setup(r => r.CreateApplicationAsync(
                    It.IsAny<ApplicationModel>(),
                    It.IsAny<IClientSessionHandle?>()))
                .ThrowsAsync(new Exception("DB error"));

            var createDto = new CreateApplicationModel
            {
                StudentId = "S1",
                StudentName = "John",
                Description = "Should fail"
            };

            // Act + Assert
            Assert.ThrowsAsync<Exception>(async () => await _service.CreateApplication(createDto));
            _sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
