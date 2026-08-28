using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Course;
using ShareService.Models.EducationPartner;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class CourseServiceTests
    {
        private const string UserId = "user-1";

        private Mock<ICourse> _courseDbMock;
        private Mock<IEducationPartner> _educationPartnerDbMock;
        private Mock<IValidator<CourseModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<ILogger<CourseService>> _loggerMock;
        private CourseService _service;

        [SetUp]
        public void Setup()
        {
            _courseDbMock = new Mock<ICourse>();
            _educationPartnerDbMock = new Mock<IEducationPartner>();
            _validatorMock = new Mock<IValidator<CourseModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _loggerMock = new Mock<ILogger<CourseService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CourseModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _educationPartnerDbMock
                .Setup(db => db.GetEducationPartnerByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new EducationPartnerModel { Id = "partner-1", Name = "Acme Uni" });

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.EducationPartnersAdd.GetDescription(),
                    PermissionKey.EducationPartnersEdit.GetDescription(),
                    PermissionKey.EducationPartnersDelete.GetDescription(),
                    PermissionKey.EducationPartnersView.GetDescription()
                });

            _service = new CourseService(
                _courseDbMock.Object,
                _educationPartnerDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task CreateCourse_ReturnsTrue_WhenCreated()
        {
            _courseDbMock.Setup(db => db.CreateCourseAsync(It.IsAny<CourseModel>())).ReturnsAsync(true);

            var course = new CourseModel { CourseName = "IT Degree", EducationPartnerId = "partner-1" };

            var result = await _service.CreateCourse(course, UserId);

            Assert.That(result, Is.True);
            Assert.That(course.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateCourse_Throws_WhenEducationPartnerNotFound()
        {
            _educationPartnerDbMock
                .Setup(db => db.GetEducationPartnerByIdAsync("missing-partner"))
                .ReturnsAsync((EducationPartnerModel)null);

            var course = new CourseModel { CourseName = "IT Degree", EducationPartnerId = "missing-partner" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCourse(course, UserId));

            Assert.That(ex!.Message, Does.Contain("Education partner not found"));
        }

        [Test]
        public void CreateCourse_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CourseModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("CourseName", "CourseName is required")
                }));

            var course = new CourseModel { CourseName = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCourse(course, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateCourse_Throws_WhenNotFound()
        {
            _courseDbMock.Setup(db => db.GetCourseByIdAsync("missing")).ReturnsAsync((CourseModel)null);

            var course = new CourseModel { CourseName = "IT Degree", EducationPartnerId = "partner-1" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateCourse("missing", course, UserId));

            Assert.That(ex!.Message, Does.Contain("Course not found"));
        }

        [Test]
        public async Task DeleteCourse_ReturnsTrue_WhenDeleted()
        {
            _courseDbMock.Setup(db => db.DeleteCourseAsync("1")).ReturnsAsync(true);

            var result = await _service.DeleteCourse("1", UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GetCoursesByPartnerId_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetCoursesByPartnerId("partner-1", UserId));
        }

        [Test]
        public async Task GetCoursesByPartnerIds_GroupsCoursesByPartner_NoPermissionRequired()
        {
            _courseDbMock
                .Setup(db => db.GetByPartnerIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<CourseModel>
                {
                    new() { Id = "c1", EducationPartnerId = "partner-1" },
                    new() { Id = "c2", EducationPartnerId = "partner-1" },
                    new() { Id = "c3", EducationPartnerId = "partner-2" }
                });

            var result = await _service.GetCoursesByPartnerIds(new[] { "partner-1", "partner-2" });

            Assert.That(result["partner-1"], Has.Count.EqualTo(2));
            Assert.That(result["partner-2"], Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SearchCourses_FiltersByCourseNameAndMaxTuition()
        {
            _courseDbMock.Setup(db => db.GetAllAsync()).ReturnsAsync(new List<CourseModel>
            {
                new() { Id = "c1", CourseName = "IT Degree", EducationPartnerId = "partner-1", TuitionFee = 30000 },
                new() { Id = "c2", CourseName = "Business Degree", EducationPartnerId = "partner-1", TuitionFee = 20000 }
            });
            _educationPartnerDbMock
                .Setup(db => db.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<EducationPartnerModel> { new() { Id = "partner-1", Name = "Acme Uni", Country = "AU" } });

            var filter = new CourseSearchFilter { CourseName = "IT", MaxTuition = 35000 };

            var result = await _service.SearchCourses(filter, UserId);

            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Course.CourseName, Is.EqualTo("IT Degree"));
        }
    }
}
