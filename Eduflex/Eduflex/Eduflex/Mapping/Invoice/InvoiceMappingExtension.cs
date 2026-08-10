using Eduflex.DTOs.Invoice;
using ShareService.Models.Invoice;

namespace Eduflex.Mapping.Invoice
{
    public static class InvoiceMappingExtension
    {
        public static InvoiceClaimItemDto ToDto(this ShareService.Models.Invoice.InvoiceClaimItem item)
        {
            return new InvoiceClaimItemDto { Description = item.Description, Amount = item.Amount, GstRatePercent = item.GstRatePercent };
        }

        public static ShareService.Models.Invoice.InvoiceClaimItem ToModel(this InvoiceClaimItemDto dto)
        {
            return new ShareService.Models.Invoice.InvoiceClaimItem { Description = dto.Description, Amount = dto.Amount, GstRatePercent = dto.GstRatePercent };
        }

        public static InvoiceRecordDto ToDto(this InvoiceModel model)
        {
            return new InvoiceRecordDto
            {
                Id = model.Id,
                InvoiceNo = model.InvoiceNo,
                TemplateId = model.TemplateId,
                Category = model.Category,
                RecipientType = model.RecipientType,
                RecipientId = model.RecipientId,
                RecipientName = model.RecipientName,
                RecipientEmail = model.RecipientEmail,
                RelatedEnrolmentId = model.RelatedEnrolmentId,
                RelatedStepKey = model.RelatedStepKey,
                RelatedFinancialRecordId = model.RelatedFinancialRecordId,
                Description = model.Description,
                Amount = model.Amount,
                GstAmount = model.GstAmount,
                Total = model.Total,
                ClaimItems = model.ClaimItems.Select(i => i.ToDto()).ToList(),
                StudentName = model.StudentName,
                StudentRefCode = model.StudentRefCode,
                StudentEmail = model.StudentEmail,
                StudentPhone = model.StudentPhone,
                CourseName = model.CourseName,
                Campus = model.Campus,
                EducationPartnerName = model.EducationPartnerName,
                CustomContent = model.CustomContent,
                PdfUrl = model.PdfUrl,
                PdfFileName = model.PdfFileName,
                Status = model.Status,
                SentAt = model.SentAt,
                PaidAt = model.PaidAt,
                PaymentEvidenceUrl = model.PaymentEvidenceUrl,
                LastEmailError = model.LastEmailError,
                CancelledAt = model.CancelledAt,
                CancelReason = model.CancelReason,
                EmailSubject = model.EmailSubject,
                EmailBody = model.EmailBody,
                CreatedByName = model.CreatedByName
            };
        }

        public static ShareService.Models.Invoice.SendInvoiceRequestModel ToModel(this SendInvoiceDto dto)
        {
            return new ShareService.Models.Invoice.SendInvoiceRequestModel
            {
                TemplateId = dto.TemplateId,
                RecipientType = dto.RecipientType,
                RecipientId = dto.RecipientId,
                RecipientName = dto.RecipientName,
                RecipientEmail = dto.RecipientEmail,
                RelatedEnrolmentId = dto.RelatedEnrolmentId,
                RelatedStepKey = dto.RelatedStepKey,
                RelatedFinancialRecordId = dto.RelatedFinancialRecordId,
                RelatedInvoicePlanEntryId = dto.RelatedInvoicePlanEntryId,
                RelatedStudentPlanEntryId = dto.RelatedStudentPlanEntryId,
                Description = dto.Description,
                Amount = dto.Amount,
                GstRatePercent = dto.GstRatePercent,
                ClaimItems = dto.ClaimItems.Select(i => i.ToModel()).ToList(),
                StudentName = dto.StudentName,
                StudentRefCode = dto.StudentRefCode,
                StudentEmail = dto.StudentEmail,
                StudentPhone = dto.StudentPhone,
                CourseName = dto.CourseName,
                Campus = dto.Campus,
                EducationPartnerName = dto.EducationPartnerName,
                CustomContent = dto.CustomContent,
                EmailSubject = dto.EmailSubject,
                EmailBody = dto.EmailBody
            };
        }
    }
}
