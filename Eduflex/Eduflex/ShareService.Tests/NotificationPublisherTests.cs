using Moq;
using NUnit.Framework;
using ShareService.DataAccess.Interface;
using ShareService.Enums.Roles;
using ShareService.Messaging;
using ShareService.Models.Department;
using ShareService.Models.Notification;
using ShareService.Models.Role;
using ShareService.Services;
using ShareService.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareService.Tests
{
    [TestFixture]
    public class NotificationPublisherTests
    {
        private Mock<INotification> _notificationDbMock;
        private Mock<IDepartment> _departmentDbMock;
        private Mock<IUserDB> _userDbMock;
        private Mock<IRoleService> _roleServiceMock;
        private Mock<INotificationBroadcaster> _broadcasterMock;
        private NotificationPublisher _publisher;

        [SetUp]
        public void Setup()
        {
            _notificationDbMock = new Mock<INotification>();
            _departmentDbMock = new Mock<IDepartment>();
            _userDbMock = new Mock<IUserDB>();
            _roleServiceMock = new Mock<IRoleService>();
            _broadcasterMock = new Mock<INotificationBroadcaster>();

            _notificationDbMock
                .Setup(db => db.CreateNotificationAsync(It.IsAny<NotificationModel>()))
                .ReturnsAsync(true);

            _publisher = new NotificationPublisher(
                _notificationDbMock.Object,
                _departmentDbMock.Object,
                _userDbMock.Object,
                _roleServiceMock.Object,
                _broadcasterMock.Object
            );
        }

        [Test]
        public async Task PublishAsync_DoesNothing_WhenStaffTargetHasNoUserIds()
        {
            await _publisher.PublishAsync("Enquiry", "e1", "New enquiry", NotificationTarget.ToStaff(new List<string>()));

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(It.IsAny<NotificationModel>()), Times.Never);
            _broadcasterMock.Verify(b => b.BroadcastAsync(It.IsAny<NotificationMessage>()), Times.Never);
        }

        [Test]
        public async Task PublishAsync_CreatesAndBroadcasts_ForStaffTarget()
        {
            await _publisher.PublishAsync("Enquiry", "e1", "New enquiry", NotificationTarget.ToStaff(new List<string> { "staff-1", "staff-2" }));

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(
                It.Is<NotificationModel>(n => n.RecipientUserIds.Count == 2)), Times.Once);
            _broadcasterMock.Verify(b => b.BroadcastAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Test]
        public async Task PublishAsync_ResolvesDepartmentHead_ForDepartmentHeadTarget()
        {
            _departmentDbMock.Setup(db => db.GetDepartmentByIdAsync("dept-1"))
                .ReturnsAsync(new DepartmentModel { Id = "dept-1", HeadUserId = "head-1" });

            await _publisher.PublishAsync("Task", "t1", "New task", NotificationTarget.ToDepartmentHead("dept-1"));

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(
                It.Is<NotificationModel>(n => n.RecipientUserIds.Count == 1 && n.RecipientUserIds[0] == "head-1")), Times.Once);
        }

        [Test]
        public async Task PublishAsync_DoesNothing_WhenDepartmentHeadNotAssigned()
        {
            _departmentDbMock.Setup(db => db.GetDepartmentByIdAsync("dept-1"))
                .ReturnsAsync(new DepartmentModel { Id = "dept-1", HeadUserId = null });

            await _publisher.PublishAsync("Task", "t1", "New task", NotificationTarget.ToDepartmentHead("dept-1"));

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(It.IsAny<NotificationModel>()), Times.Never);
        }

        [Test]
        public async Task PublishAsync_ResolvesDepartmentMembers_ForDepartmentTarget()
        {
            _departmentDbMock.Setup(db => db.GetDepartmentByIdAsync("dept-1"))
                .ReturnsAsync(new DepartmentModel { Id = "dept-1", MemberUserIds = new List<string> { "m1", "m2", "m3" } });

            await _publisher.PublishAsync("Task", "t1", "New task", NotificationTarget.ToDepartment("dept-1"));

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(
                It.Is<NotificationModel>(n => n.RecipientUserIds.Count == 3)), Times.Once);
        }

        [Test]
        public async Task PublishToRoleAsync_ResolvesUsersByRole()
        {
            _roleServiceMock.Setup(r => r.GetByNameAsync("Staff"))
                .ReturnsAsync(new RoleModel { Id = "role-staff", Name = "Staff" });
            _userDbMock.Setup(db => db.GetUserIdsByRoleIdAsync("role-staff"))
                .ReturnsAsync(new List<string> { "staff-1", "staff-2" });

            await _publisher.PublishToRoleAsync("Enquiry", "e1", "New enquiry", SystemRole.Staff);

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(
                It.Is<NotificationModel>(n => n.RecipientUserIds.Count == 2)), Times.Once);
        }

        [Test]
        public async Task PublishToRoleAsync_NoOps_WhenRoleNotConfigured()
        {
            _roleServiceMock.Setup(r => r.GetByNameAsync("Staff")).ReturnsAsync((RoleModel)null);

            await _publisher.PublishToRoleAsync("Enquiry", "e1", "New enquiry", SystemRole.Staff);

            _notificationDbMock.Verify(db => db.CreateNotificationAsync(It.IsAny<NotificationModel>()), Times.Never);
            _userDbMock.Verify(db => db.GetUserIdsByRoleIdAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
