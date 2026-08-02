namespace Eduflex.DTOs.Notification
{
    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string? TargetDepartmentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
