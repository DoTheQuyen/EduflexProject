namespace Eduflex.DTOs.Invoice
{
    public class InvoiceTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? LogoUrl { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public List<string> SenderAddressLines { get; set; } = new();
        public string? SenderAbn { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string? SenderPhone { get; set; }
        public string? BankName { get; set; }
        public string? BankBsb { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
        public string InvoiceNoPrefix { get; set; } = string.Empty;
        public int NumberPadding { get; set; }
        public int NextSequence { get; set; }
        // Convenience for the admin UI's "next number will be..." preview — computed
        // server-side so the frontend doesn't need to duplicate the padding format logic.
        public string NextInvoiceNoPreview { get; set; } = string.Empty;
        public string? DefaultDescription { get; set; }
        public decimal? DefaultAmount { get; set; }
        public decimal? DefaultGstRatePercent { get; set; }
    }

    public class CreateInvoiceTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public List<string> SenderAddressLines { get; set; } = new();
        public string? SenderAbn { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string? SenderPhone { get; set; }
        public string? BankName { get; set; }
        public string? BankBsb { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
        public string InvoiceNoPrefix { get; set; } = "INV-Eduflex";
        public int NumberPadding { get; set; } = 4;
        public string? DefaultDescription { get; set; }
        public decimal? DefaultAmount { get; set; }
        public decimal? DefaultGstRatePercent { get; set; }
    }

    public class UpdateInvoiceTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public List<string> SenderAddressLines { get; set; } = new();
        public string? SenderAbn { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string? SenderPhone { get; set; }
        public string? BankName { get; set; }
        public string? BankBsb { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
        public string? DefaultDescription { get; set; }
        public decimal? DefaultAmount { get; set; }
        public decimal? DefaultGstRatePercent { get; set; }
    }

    public class SetInvoiceTemplateStatusDto
    {
        public bool IsActive { get; set; }
    }
}
