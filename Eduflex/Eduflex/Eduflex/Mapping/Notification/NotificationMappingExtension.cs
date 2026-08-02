using Eduflex.DTOs.Notification;
using ShareService.Models.Notification;

namespace Eduflex.Mapping.Notification
{
    public static class NotificationMappingExtension
    {
        public static NotificationDto ToDto(this NotificationModel model)
        {
            return new NotificationDto
            {
                Id = model.Id,
                Module = model.Module,
                EntityId = model.EntityId,
                Summary = model.Summary,
                TargetType = model.TargetType,
                TargetDepartmentId = model.TargetDepartmentId,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
