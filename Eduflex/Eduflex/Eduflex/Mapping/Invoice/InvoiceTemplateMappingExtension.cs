using Eduflex.DTOs.Invoice;
using ShareService.Models.Invoice;

namespace Eduflex.Mapping.Invoice
{
    public static class InvoiceTemplateMappingExtension
    {
        public static InvoiceTemplateDto ToDto(this InvoiceTemplateModel model)
        {
            return new InvoiceTemplateDto
            {
                Id = model.Id,
                Name = model.Name,
                Category = model.Category,
                IsActive = model.IsActive,
                LogoUrl = model.LogoUrl,
                SenderName = model.SenderName,
                SenderAddressLines = model.SenderAddressLines,
                SenderAbn = model.SenderAbn,
                SenderEmail = model.SenderEmail,
                SenderPhone = model.SenderPhone,
                BankName = model.BankName,
                BankBsb = model.BankBsb,
                BankAccountNumber = model.BankAccountNumber,
                BankAccountName = model.BankAccountName,
                InvoiceNoPrefix = model.InvoiceNoPrefix,
                NumberPadding = model.NumberPadding,
                NextSequence = model.NextSequence,
                NextInvoiceNoPreview = model.FormatInvoiceNo(model.NextSequence),
                DefaultDescription = model.DefaultDescription,
                DefaultAmount = model.DefaultAmount,
                DefaultGstRatePercent = model.DefaultGstRatePercent
            };
        }

        public static InvoiceTemplateModel ToModel(this CreateInvoiceTemplateDto dto)
        {
            return new InvoiceTemplateModel
            {
                Name = dto.Name,
                Category = dto.Category,
                LogoUrl = dto.LogoUrl,
                SenderName = dto.SenderName,
                SenderAddressLines = dto.SenderAddressLines,
                SenderAbn = dto.SenderAbn,
                SenderEmail = dto.SenderEmail,
                SenderPhone = dto.SenderPhone,
                BankName = dto.BankName,
                BankBsb = dto.BankBsb,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                InvoiceNoPrefix = dto.InvoiceNoPrefix,
                NumberPadding = dto.NumberPadding,
                DefaultDescription = dto.DefaultDescription,
                DefaultAmount = dto.DefaultAmount,
                DefaultGstRatePercent = dto.DefaultGstRatePercent
            };
        }

        public static InvoiceTemplateModel ToModel(this UpdateInvoiceTemplateDto dto)
        {
            return new InvoiceTemplateModel
            {
                Name = dto.Name,
                LogoUrl = dto.LogoUrl,
                SenderName = dto.SenderName,
                SenderAddressLines = dto.SenderAddressLines,
                SenderAbn = dto.SenderAbn,
                SenderEmail = dto.SenderEmail,
                SenderPhone = dto.SenderPhone,
                BankName = dto.BankName,
                BankBsb = dto.BankBsb,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                DefaultDescription = dto.DefaultDescription,
                DefaultAmount = dto.DefaultAmount,
                DefaultGstRatePercent = dto.DefaultGstRatePercent
            };
        }
    }
}
