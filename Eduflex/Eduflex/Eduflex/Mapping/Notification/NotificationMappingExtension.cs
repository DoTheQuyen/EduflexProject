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

        public static DashboardSummaryDto ToDto(this DashboardSummaryModel model)
        {
            return new DashboardSummaryDto
            {
                Notifications = model.Notifications.Select(n => n.ToDto()).ToList(),
                Counts = model.Counts
            };
        }

        public static MonthlyTrendDto ToDto(this MonthlyTrendModel model)
        {
            return new MonthlyTrendDto
            {
                Points = model.Points.Select(p => new MonthlyTrendPointDto
                {
                    Month = p.Month,
                    Enquiry = p.Enquiry,
                    Application = p.Application,
                    Enrolment = p.Enrolment,
                    MigrationCase = p.MigrationCase
                }).ToList()
            };
        }

        public static StatusCountDto ToDto(this StatusCountModel model)
        {
            return new StatusCountDto
            {
                Status = model.Status,
                Label = model.Label,
                Count = model.Count
            };
        }

        public static StatusBreakdownDto ToDto(this StatusBreakdownModel model)
        {
            return new StatusBreakdownDto
            {
                Enquiry = model.Enquiry.Select(s => s.ToDto()).ToList(),
                Application = model.Application.Select(s => s.ToDto()).ToList(),
                Enrolment = model.Enrolment.Select(s => s.ToDto()).ToList(),
                MigrationCase = model.MigrationCase.Select(s => s.ToDto()).ToList()
            };
        }
    }
}
