using Eduflex.DTOs.Invoice;
using ShareService.Models.Invoice;

namespace Eduflex.Mapping.Invoice
{
    public static class InvoiceMappingExtension
    {
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
                Description = model.Description,
                Amount = model.Amount,
                GstAmount = model.GstAmount,
                Total = model.Total,
                PdfUrl = model.PdfUrl,
                PdfFileName = model.PdfFileName,
                Status = model.Status,
                SentAt = model.SentAt,
                PaidAt = model.PaidAt,
                PaymentEvidenceUrl = model.PaymentEvidenceUrl,
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
                Description = dto.Description,
                Amount = dto.Amount,
                GstRatePercent = dto.GstRatePercent,
                EmailSubject = dto.EmailSubject,
                EmailBody = dto.EmailBody
            };
        }
    }
}
