using Eduflex.DTOs.StudentPaymentPlan;
using ShareService.Models.StudentPaymentPlan;

namespace Eduflex.Mapping.StudentPaymentPlan
{
    public static class StudentPaymentPlanEntryMappingExtension
    {
        public static StudentPaymentPlanEntryDto ToDto(this StudentPaymentPlanEntryModel model)
        {
            return new StudentPaymentPlanEntryDto
            {
                Id = model.Id,
                EnrolmentId = model.EnrolmentId,
                StudentName = model.StudentName,
                CourseName = model.CourseName,
                FeeType = model.FeeType,
                Label = model.Label,
                InstalmentNumber = model.InstalmentNumber,
                TotalInstalments = model.TotalInstalments,
                DueDate = model.DueDate,
                Amount = model.Amount,
                Status = model.Status,
                LinkedInvoiceId = model.LinkedInvoiceId,
                SkipReason = model.SkipReason,
                IsManual = model.IsManual
            };
        }
    }
}
