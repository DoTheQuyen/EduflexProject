export interface CommissionAdjustment {
  id: string;
  reason: string;
  amount: number;
  addedByName: string;
  addedAt: string;
}

export interface InvoicePlanEntry {
  plannedRequestDate: string;
  status: 'Planned' | 'Invoiced' | 'Skipped';
  linkedInvoiceId?: string;
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
