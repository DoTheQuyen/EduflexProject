import { InvoiceClaimItem } from './invoice';

export interface CommissionAdjustment {
  id: string;
  reason: string;
  amount: number;
  addedByName: string;
  addedAt: string;
}

export interface InvoicePlanEntry {
  id: string;
  // The course intake this claim is tied to — absent for manually-added claims.
  intakeDate?: string;
  // Editable — defaults to intakeDate + 45 days when generated.
  claimDate: string;
  status: 'Planned' | 'Invoiced' | 'Skipped';
  linkedInvoiceId?: string;
  skipReason?: string;
  isManual: boolean;
}

export type InvoiceToType = 'BusinessPartner' | 'EducationPartner';
export type InvoiceStatus = 'Draft' | 'Generated';

export interface Invoice {
  id: string;
  invoiceNo: string;
  invoiceToType: InvoiceToType;
  invoiceToId: string;
  invoiceToName: string;
  studentName: string;
  periodStart: string;
  periodEnd: string;
  periodTotal: number;
  htmlContent: string;
  status: InvoiceStatus;
  pdfUrl?: string;
  pdfFileName?: string;
  generatedAt?: string;
  createdByName: string;
  createdAt: string;
}

export type FinancialRecipientType = 'EducationPartner' | 'BusinessPartner';

export interface FinancialCommunication {
  id: string;
  templateKey?: string;
  toEmail: string;
  recipientType: FinancialRecipientType;
  subject: string;
  body: string;
  relatedInvoiceId?: string;
  sentByName: string;
  sentAt: string;
}

export interface FinancialAuditEntry {
  id: string;
  description: string;
  performedByName: string;
  performedAt: string;
}

export interface FinancialRecord {
  id: string;
  enrolmentId: string;
  studentName: string;
  enrolmentStatus: string;
  educationPartnerId?: string;
  businessPartnerId?: string;
  courseCommissionRate: number;
  businessPartnerCommissionRate: number;
  totalTuition: number;
  expectedCommission: number;
  extraCommissionAdjustments: CommissionAdjustment[];
  invoicePlan: InvoicePlanEntry[];
  invoices: Invoice[];
  communications: FinancialCommunication[];
  auditTrail: FinancialAuditEntry[];
  createdAt: string;
}

export interface FinancialRecordFilter {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
}

export interface CreateInvoiceDraftRequest {
  invoiceNo: string;
  invoiceToType: InvoiceToType;
  invoiceToId: string;
  invoiceToName: string;
  studentName: string;
  periodStart: string;
  periodEnd: string;
  periodTotal: number;
  htmlContent: string;
}

// The new Invoice module's shape (ShareService.Models.Invoice.InvoiceModel /
// Eduflex.DTOs.Invoice.InvoiceRecordDto) — distinct from the old embedded `Invoice`
// above, which the Communication tab still reads for now. Declared locally rather than
// via the NSwag-generated Client so this doesn't wait on a `nswag run` cycle — same
// reasoning as FinancialRecordService's other hand-written calls.
export interface FinanceInvoice {
  id: string;
  invoiceNo: string;
  category: string;
  recipientType: 'BusinessPartner' | 'EducationPartner';
  recipientId?: string;
  recipientName: string;
  recipientEmail: string;
  description: string;
  amount: number;
  gstAmount: number;
  total: number;
  claimItems: InvoiceClaimItem[];
  studentName?: string;
  studentRefCode?: string;
  studentEmail?: string;
  studentPhone?: string;
  courseName?: string;
  campus?: string;
  educationPartnerName?: string;
  pdfUrl?: string;
  pdfFileName?: string;
  // Failed = invoice + PDF exist but the email didn't go out (retry with Resend).
  // Cancelled = soft void, excluded from income totals. Neither counts as income.
  status: 'Sent' | 'Paid' | 'Failed' | 'Cancelled';
  sentAt: string;
  paidAt?: string;
  paymentEvidenceUrl?: string;
  lastEmailError?: string;
  cancelledAt?: string;
  cancelReason?: string;
  // Last message sent for this invoice — prefills the Resend dialog. Empty on invoices
  // created before these were persisted.
  emailSubject: string;
  emailBody: string;
  createdByName: string;
}

export interface SendFinanceInvoiceRequest {
  templateId: string;
  recipientType: 'BusinessPartner' | 'EducationPartner';
  recipientId: string;
  recipientName: string;
  recipientEmail: string;
  relatedFinancialRecordId: string;
  relatedInvoicePlanEntryId?: string;
  description: string;
  amount: number;
  gstRatePercent: number;
  claimItems: InvoiceClaimItem[];
  studentName?: string;
  studentRefCode?: string;
  studentEmail?: string;
  studentPhone?: string;
  courseName?: string;
  campus?: string;
  educationPartnerName?: string;
  emailSubject: string;
  emailBody: string;
}
