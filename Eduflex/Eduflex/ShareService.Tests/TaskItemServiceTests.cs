using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Enums.Task;
using ShareService.Models.Auth;
using ShareService.Models.Department;
using ShareService.Models.Role;
using ShareService.Models.Task;
using ShareService.Services;
using ShareService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class TaskItemServiceTests
    {
        private const string UserId = "user-1";

        private Mock<ITaskItem> _taskDbMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<IDepartmentService> _departmentServiceMock;
        private Mock<IEnrolment> _enrolmentDbMock;
        private Mock<IEnquiry> _enquiryDbMock;
        private Mock<IApplication> _applicationDbMock;
        private Mock<IFinancialRecord> _financialRecordDbMock;
        private Mock<IMigrationCase> _migrationCaseDbMock;
        private Mock<IValidator<TaskItemModel>> _validatorMock;
        private Mock<IPermissionService> _permissionServiceMock;
        private Mock<INotificationPublisher> _notificationPublisherMock;
        private Mock<ILogger<TaskItemService>> _loggerMock;
        private TaskItemService _service;

        [SetUp]
        public void Setup()
        {
            _taskDbMock = new Mock<ITaskItem>();
            _userServiceMock = new Mock<IUserService>();
            _roleServiceMock = new Mock<IRoleService>();
            _departmentServiceMock = new Mock<IDepartmentService>();
            _enrolmentDbMock = new Mock<IEnrolment>();
            _enquiryDbMock = new Mock<IEnquiry>();
            _applicationDbMock = new Mock<IApplication>();
            _financialRecordDbMock = new Mock<IFinancialRecord>();
            _migrationCaseDbMock = new Mock<IMigrationCase>();
            _validatorMock = new Mock<IValidator<TaskItemModel>>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _notificationPublisherMock = new Mock<INotificationPublisher>();
            _loggerMock = new Mock<ILogger<TaskItemService>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<TaskItemModel>(), default))
                .ReturnsAsync(new ValidationResult());

            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string>
                {
                    PermissionKey.TasksView.GetDescription(),
                    PermissionKey.TasksViewAll.GetDescription(),
                    PermissionKey.TasksAdd.GetDescription(),
                    PermissionKey.TasksEdit.GetDescription()
                });

            _userServiceMock
                .Setup(u => u.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new UserModel { Id = id, FirstName = "Assignee", LastName = "Staff", RoleId = "role-staff" });
            _roleServiceMock
                .Setup(r => r.GetByIdAsync("role-staff"))
                .ReturnsAsync(new RoleModel { Id = "role-staff", RoleType = RoleTypeEnums.Staff });

            _service = new TaskItemService(
                _taskDbMock.Object,
                _userServiceMock.Object,
                _roleServiceMock.Object,
                _departmentServiceMock.Object,
                _enrolmentDbMock.Object,
                _enquiryDbMock.Object,
                _applicationDbMock.Object,
                _financialRecordDbMock.Object,
                _migrationCaseDbMock.Object,
                _validatorMock.Object,
                _permissionServiceMock.Object,
                _notificationPublisherMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task SearchAllTasksAsync_ReturnsEmptyPage_WhenUserManagesNoDepartments()
        {
            _departmentServiceMock
                .Setup(d => d.GetDepartmentsManagedByUserAsync(UserId))
                .ReturnsAsync(new List<DepartmentModel>());

            var result = await _service.SearchAllTasksAsync(new TaskItemFilter(), UserId);

            Assert.That(result.Items, Is.Empty);
            _taskDbMock.Verify(db => db.SearchTasksAsync(It.IsAny<TaskItemFilter>(), It.IsAny<List<string>>()), Times.Never);
        }

        [Test]
        public void SearchLinkedTasksAsync_Throws_WhenNoLinkedRecordIdSupplied()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SearchLinkedTasksAsync(new TaskItemFilter(), UserId));

            Assert.That(ex!.Message, Does.Contain("linked record id"));
        }

        [Test]
        public void GetTaskByIdAsync_Throws_WhenNotInvolvedAndLacksViewAllPermission()
        {
            _permissionServiceMock
                .Setup(p => p.GetPermissionsForUserAsync(UserId))
                .ReturnsAsync(new List<string> { PermissionKey.TasksView.GetDescription() });
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1"))
                .ReturnsAsync(new TaskItemModel { Id = "1", AssignerUserId = "other-1", AssigneeUserId = "other-2" });

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetTaskByIdAsync("1", UserId));
        }

        [Test]
        public void CreateTaskAsync_Throws_WhenAssigneeIsStudent()
        {
            _userServiceMock
                .Setup(u => u.GetUserByIdAsync("student-1"))
                .ReturnsAsync(new UserModel { Id = "student-1", RoleId = "role-student" });
            _roleServiceMock
                .Setup(r => r.GetByIdAsync("role-student"))
                .ReturnsAsync(new RoleModel { Id = "role-student", RoleType = RoleTypeEnums.Student });

            var task = new TaskItemModel { Name = "Follow up", AssigneeUserId = "student-1" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTaskAsync(task, UserId));

            Assert.That(ex!.Message, Does.Contain("cannot be assigned to a student"));
        }

        [Test]
        public async Task CreateTaskAsync_SetsAssignerAndDefaultStatus_WhenCreated()
        {
            _taskDbMock.Setup(db => db.CreateTaskAsync(It.IsAny<TaskItemModel>())).ReturnsAsync(true);

            var task = new TaskItemModel { Name = "Follow up", AssigneeUserId = "assignee-1" };

            var result = await _service.CreateTaskAsync(task, UserId);

            Assert.That(result, Is.True);
            Assert.That(task.AssignerUserId, Is.EqualTo(UserId));
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.New.ToString()));
        }

        [Test]
        public void UpdateTaskAsync_Throws_WhenCallerIsNotAssigner()
        {
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1"))
                .ReturnsAsync(new TaskItemModel { Id = "1", AssignerUserId = "other-1", AssigneeUserId = "assignee-1" });

            var task = new TaskItemModel { Name = "Follow up" };

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.UpdateTaskAsync("1", task, UserId));
        }

        [Test]
        public void UpdateTaskAsync_Throws_WhenTaskCompleted()
        {
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1"))
                .ReturnsAsync(new TaskItemModel { Id = "1", AssignerUserId = UserId, Status = TaskItemStatus.Completed.ToString() });

            var task = new TaskItemModel { Name = "Follow up" };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateTaskAsync("1", task, UserId));

            Assert.That(ex!.Message, Does.Contain("read-only"));
        }

        [Test]
        public void ReassignTaskAsync_Throws_WhenNoteEmpty()
        {
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1"))
                .ReturnsAsync(new TaskItemModel { Id = "1", AssignerUserId = UserId, AssigneeUserId = "assignee-1" });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReassignTaskAsync("1", "new-assignee", "", UserId));

            Assert.That(ex!.Message, Does.Contain("note is required"));
        }

        [Test]
        public void ChangeStatusAsync_Throws_WhenTransitionInvalid()
        {
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1"))
                .ReturnsAsync(new TaskItemModel { Id = "1", AssignerUserId = UserId, Status = TaskItemStatus.New.ToString() });

            var ex = Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ChangeStatusAsync("1", TaskItemStatus.New, UserId));

            Assert.That(ex!.Message, Does.Contain("Cannot change status"));
        }

        [Test]
        public async Task ChangeStatusAsync_AllowsReopen_FromCompletedToProcessing()
        {
            var task = new TaskItemModel { Id = "1", AssignerUserId = UserId, Status = TaskItemStatus.Completed.ToString() };
            _taskDbMock.Setup(db => db.GetTaskByIdAsync("1")).ReturnsAsync(task);
            _taskDbMock.Setup(db => db.ReplaceTaskAsync("1", It.IsAny<TaskItemModel>())).ReturnsAsync(true);

            var result = await _service.ChangeStatusAsync("1", TaskItemStatus.Processing, UserId);

            Assert.That(result, Is.True);
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.Processing.ToString()));
        }
    }
}
