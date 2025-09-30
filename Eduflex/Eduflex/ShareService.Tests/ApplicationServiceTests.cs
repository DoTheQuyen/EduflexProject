using Moq;
using NUnit.Framework;
using ShareService.Models;
using ShareService.DataAccess.Interface;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class ApplicationServiceTests
    {
        private Mock<IApplication> _appRepoMock;
        private Mock<IValidator<CreateApplicationModel>> _validatorMock;
        private Mock<ILogger<ApplicationService>> _loggerMock;
        private ApplicationService _service;

        [SetUp]
        public void Setup()
        {
            _appRepoMock = new Mock<IApplication>();
            _validatorMock = new Mock<IValidator<CreateApplicationModel>>();
            _loggerMock = new Mock<ILogger<ApplicationService>>();

            // Validator always passes
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateApplicationModel>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _service = new ApplicationService(_appRepoMock.Object, _validatorMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetApplicationsByStudentId_ReturnsApplications()
        {
            _appRepoMock.Setup(r => r.GetApplicationsByStudentIdAsync("S1"))
                .ReturnsAsync(new List<ApplicationModel>
                {
                    new ApplicationModel { Id = "1", Description = "App1", Status = "Pending" }
                });

            var result = await _service.GetApplicationsByStudentId("S1");

            Assert.That(1, Is.EqualTo(result.Count));
            Assert.That("App1", Is.EqualTo(result[0].Description));
        }

        //[Test]
        //public void GetApplicationsByStudentId_Throws_WhenEmptyId()
        //{
        //    Assert.ThrowsAsync<ArgumentException>(() => _service.GetApplicationsByStudentId(""));
        //}

        [Test]
        public async Task CreateApplication_SetsDefaultStatus()
        {
            var createDto = new CreateApplicationModel { StudentId = "S1", StudentName = "John", Description = "Test" };

            _appRepoMock.Setup(r => r.CreateApplicationAsync(It.IsAny<ApplicationModel>()))
                .ReturnsAsync((ApplicationModel app) => app);

            var result = await _service.CreateApplication(createDto);

            Assert.That("Pending", Is.EqualTo(result.Status));
        }
    }
}
