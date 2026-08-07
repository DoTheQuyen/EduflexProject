using Eduflex.DTOs.Financial;
using ShareService.Models.Financial;

namespace Eduflex.Mapping.Financial
{
    public static class FinancialMappingExtension
    {
        public static FinancialRecordFilter ToFilter(this FinancialRecordFilterDto dto)
        {
            return new FinancialRecordFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

        public static CommissionAdjustmentDto ToDto(this CommissionAdjustmentModel model)
        {
            return new CommissionAdjustmentDto
            {
                Id = model.Id,
                Reason = model.Reason,
                Amount = model.Amount,
                AddedByName = model.AddedByName,
                AddedAt = model.AddedAt
            };
        }

        public static InvoicePlanEntryDto ToDto(this InvoicePlanEntryModel model)
        {
            return new InvoicePlanEntryDto
            {
                Id = model.Id,
                IntakeDate = model.IntakeDate,
                ClaimDate = model.ClaimDate,
                Status = model.Status,
                LinkedInvoiceId = model.LinkedInvoiceId,
                SkipReason = model.SkipReason,
                IsManual = model.IsManual
            };
        }

        public static InvoiceDto ToDto(this InvoiceModel model)
        {
            return new InvoiceDto
            {
                Id = model.Id,
                InvoiceNo = model.InvoiceNo,
                InvoiceToType = model.InvoiceToType,
                InvoiceToId = model.InvoiceToId,
                InvoiceToName = model.InvoiceToName,
                StudentName = model.StudentName,
                PeriodStart = model.PeriodStart,
                PeriodEnd = model.PeriodEnd,
                PeriodTotal = model.PeriodTotal,
                HtmlContent = model.HtmlContent,
                Status = model.Status,
                PdfUrl = model.PdfUrl,
                PdfFileName = model.PdfFileName,
                GeneratedAt = model.GeneratedAt,
                CreatedByName = model.CreatedByName,
                CreatedAt = model.CreatedAt
            };
        }

        public static FinancialCommunicationDto ToDto(this FinancialCommunicationModel model)
        {
            return new FinancialCommunicationDto
            {
                Id = model.Id,
                TemplateKey = model.TemplateKey,
                ToEmail = model.ToEmail,
                RecipientType = model.RecipientType,
                Subject = model.Subject,
                Body = model.Body,
                RelatedInvoiceId = model.RelatedInvoiceId,
                SentByName = model.SentByName,
                SentAt = model.SentAt
            };
        }

        public static FinancialAuditEntryDto ToDto(this FinancialAuditEntryModel model)
        {
            return new FinancialAuditEntryDto
            {
                Id = model.Id,
                Description = model.Description,
                PerformedByName = model.PerformedByName,
                PerformedAt = model.PerformedAt
            };
        }

        public static FinancialRecordDto ToDto(this FinancialRecordModel model, string studentName = "", string enrolmentStatus = "")
        {
            return new FinancialRecordDto
            {
                Id = model.Id,
                EnrolmentId = model.EnrolmentId,
                StudentName = studentName,
                EnrolmentStatus = enrolmentStatus,
                EducationPartnerId = model.EducationPartnerId,
                BusinessPartnerId = model.BusinessPartnerId,
                CourseCommissionRate = model.CourseCommissionRate,
                BusinessPartnerCommissionRate = model.BusinessPartnerCommissionRate,
                TotalTuition = model.TotalTuition,
                ExpectedCommission = model.ExpectedCommission,
                ExtraCommissionAdjustments = model.ExtraCommissionAdjustments.Select(a => a.ToDto()).ToList(),
                InvoicePlan = model.InvoicePlan.Select(p => p.ToDto()).ToList(),
                Invoices = model.Invoices.Select(i => i.ToDto()).ToList(),
                Communications = model.Communications.Select(c => c.ToDto()).ToList(),
                AuditTrail = model.AuditTrail.Select(a => a.ToDto()).ToList(),
                CreatedAt = model.CreatedAt
            };
        }
    }
}
