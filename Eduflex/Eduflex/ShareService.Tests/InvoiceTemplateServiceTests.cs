using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Invoice;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class InvoiceTemplateServiceTests
    {
        private const string UserId = "user-1";

        private Mock<IInvoiceTemplate> _invoiceTemplateDbMock;
        private Mock<IValidator<InvoiceTemplateModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private InvoiceTemplateService _service;

        [SetUp]
        public void Setup()
        {
            _invoiceTemplateDbMock = new Mock<IInvoiceTemplate>();
            _validatorMock = new Mock<IValidator<InvoiceTemplateModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<InvoiceTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.InvoiceTemplatesEdit.GetDescription() });

            _service = new InvoiceTemplateService(
                _invoiceTemplateDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object
            );
        }

        [Test]
        public async Task GetAllAsync_ReturnsTemplates_NoPermissionRequired()
        {
            _invoiceTemplateDbMock
                .Setup(db => db.GetAllAsync())
                .ReturnsAsync(new List<InvoiceTemplateModel> { new() { Id = "1", Name = "Tuition" } });

            var result = await _service.GetAllAsync();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetByIdAsync_Throws_WhenCallerLacksPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>());

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync("1", UserId));
        }

        [Test]
        public async Task CreateAsync_SetsDefaults_WhenCreated()
        {
            var template = new InvoiceTemplateModel { Name = "Tuition", Category = "Tuition" };

            var result = await _service.CreateAsync(template, UserId);

            Assert.That(result.Id, Is.EqualTo(string.Empty));
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.NextSequence, Is.EqualTo(1));
            _invoiceTemplateDbMock.Verify(db => db.CreateAsync(It.IsAny<InvoiceTemplateModel>()), Times.Once);
        }

        [Test]
        public void CreateAsync_Throws_WhenValidationFails()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<InvoiceTemplateModel>(), default))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Name", "Name is required")
                }));

            var template = new InvoiceTemplateModel { Name = "" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(template, UserId));

            Assert.That(ex!.Message, Does.Contain("Validation failed"));
        }

        [Test]
        public void UpdateAsync_Throws_WhenNotFound()
        {
            _invoiceTemplateDbMock
                .Setup(db => db.GetByIdAsync("missing"))
                .ReturnsAsync((InvoiceTemplateModel)null);

            var template = new InvoiceTemplateModel { Name = "Tuition" };

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync("missing", template, UserId));
        }

        [Test]
        public async Task UpdateAsync_PreservesImmutableNumberingFields_WhenUpdated()
        {
            var existing = new InvoiceTemplateModel
            {
                Id = "1",
                Name = "Old",
                Category = "Tuition",
                InvoiceNoPrefix = "TU",
                NumberPadding = 4,
                NextSequence = 7
            };
            _invoiceTemplateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _invoiceTemplateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<InvoiceTemplateModel>(t =>
                    t.Category == "Tuition" && t.InvoiceNoPrefix == "TU" && t.NumberPadding == 4 && t.NextSequence == 7)))
                .ReturnsAsync(true);

            var updateDto = new InvoiceTemplateModel
            {
                Name = "New",
                Category = "Enrolment",
                InvoiceNoPrefix = "EN",
                NumberPadding = 6,
                NextSequence = 99
            };

            var result = await _service.UpdateAsync("1", updateDto, UserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SetStatusAsync_SetsIsActive_WhenDeactivating()
        {
            var existing = new InvoiceTemplateModel { Id = "1", Name = "Tuition", IsActive = true };
            _invoiceTemplateDbMock.Setup(db => db.GetByIdAsync("1")).ReturnsAsync(existing);
            _invoiceTemplateDbMock
                .Setup(db => db.ReplaceAsync("1", It.Is<InvoiceTemplateModel>(t => t.IsActive == false)))
                .ReturnsAsync(true);

            var result = await _service.SetStatusAsync("1", false, UserId);

            Assert.That(result, Is.True);
        }
    }
}
