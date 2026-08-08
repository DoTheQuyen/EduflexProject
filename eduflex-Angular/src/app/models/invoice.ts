// Shared invoice-module types used by both the Finance module's Invoice tab
// (financial-record.ts's FinanceInvoice/SendFinanceInvoiceRequest) and the standalone
// Custom invoice send screen — kept here rather than duplicated in each.
export interface InvoiceClaimItem {
  description: string;
  amount: number;
  gstRatePercent: number;
}

export interface SendCustomInvoiceRequest {
  templateId: string;
  recipientName: string;
  recipientEmail: string;
  description: string;
  amount: number;
  gstRatePercent: number;
  customContent: string;
  emailSubject: string;
  emailBody: string;
}
