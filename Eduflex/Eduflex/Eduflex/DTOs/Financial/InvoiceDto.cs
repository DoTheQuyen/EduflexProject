namespace Eduflex.DTOs.Financial
{
    public class InvoiceDto
    {
        public string Id { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceToType { get; set; } = string.Empty;
        public string InvoiceToId { get; set; } = string.Empty;
        public string InvoiceToName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal PeriodTotal { get; set; }
        public string HtmlContent { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public string? PdfUrl { get; set; }
        public string? PdfFileName { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateInvoiceDraftDto
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceToType { get; set; } = string.Empty;
        public string InvoiceToId { get; set; } = string.Empty;
        public string InvoiceToName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal PeriodTotal { get; set; }
        public string HtmlContent { get; set; } = string.Empty;
    }

    public class UpdateInvoiceDraftDto
    {
        public string HtmlContent { get; set; } = string.Empty;
        public decimal PeriodTotal { get; set; }
    }

    public class InvoiceDownloadLinkDto
    {
        public string Url { get; set; } = string.Empty;
    }
}
