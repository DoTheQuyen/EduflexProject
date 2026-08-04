using ShareService.Models.Invoice;

namespace ShareService.Mapping
{
    public static class InvoiceTemplateMappingExtension
    {
        // Category, prefix and padding are deliberately excluded from an in-place edit —
        // changing the numbering scheme after invoices have already been issued under it
        // would make the sequence ambiguous, so those are set once at creation only.
        public static void ApplyEditableFields(this InvoiceTemplateModel existing, InvoiceTemplateModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.LogoUrl = updateModel.LogoUrl;
            existing.SenderName = updateModel.SenderName;
            existing.SenderAddressLines = updateModel.SenderAddressLines;
            existing.SenderAbn = updateModel.SenderAbn;
            existing.SenderEmail = updateModel.SenderEmail;
            existing.SenderPhone = updateModel.SenderPhone;
            existing.BankName = updateModel.BankName;
            existing.BankBsb = updateModel.BankBsb;
            existing.BankAccountNumber = updateModel.BankAccountNumber;
            existing.BankAccountName = updateModel.BankAccountName;
            existing.DefaultDescription = updateModel.DefaultDescription;
            existing.DefaultAmount = updateModel.DefaultAmount;
            existing.DefaultGstRatePercent = updateModel.DefaultGstRatePercent;
        }
    }
}
