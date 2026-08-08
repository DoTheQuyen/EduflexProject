import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  CommissionAdjustment, FinancialCommunication, FinancialRecipientType, FinancialRecord,
  FinancialRecordFilter, FinanceInvoice, SendFinanceInvoiceRequest
} from '../models/financial-record';
import { PagedResult } from '../models/enrolment';

// Hand-written against FinancialRecordsController's REST routes directly, rather than
// the NSwag-generated Client — several routes here nest two path parameters
// ({id}/invoices/{invoiceId}/...), and NSwag's generated method names for that shape
// aren't predictable without actually running the generator. Same rationale as
// EnrolmentService: once `nswag run` has been executed against the deployed
// FinancialRecordsController, this can be migrated to the generated Client if desired.
@Injectable({ providedIn: 'root' })
export class FinancialRecordService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/FinancialRecords`;

  constructor(private http: HttpClient) {}

  get(id: string): Observable<FinancialRecord> {
    return this.http.get<FinancialRecord>(`${this.baseUrl}/${id}`);
  }

  getByEnrolmentId(enrolmentId: string): Observable<FinancialRecord> {
    return this.http.get<FinancialRecord>(`${this.baseUrl}/by-enrolment/${enrolmentId}`);
  }

  search(filter: FinancialRecordFilter): Observable<PagedResult<FinancialRecord>> {
    return this.http.post<PagedResult<FinancialRecord>>(`${this.baseUrl}/search-financial-records`, filter);
  }

  addCommissionAdjustment(id: string, reason: string, amount: number): Observable<CommissionAdjustment> {
    return this.http.post<CommissionAdjustment>(`${this.baseUrl}/${id}/commission-adjustments`, { reason, amount });
  }

  regenerateInvoicePlan(id: string): Observable<FinancialRecord> {
    return this.http.post<FinancialRecord>(`${this.baseUrl}/${id}/invoice-plan/regenerate`, {});
  }

  updatePlanEntryDate(id: string, entryId: string, claimDate: string): Observable<FinancialRecord> {
    return this.http.put<FinancialRecord>(`${this.baseUrl}/${id}/invoice-plan/${entryId}/date`, { claimDate });
  }

  skipPlanEntry(id: string, entryId: string, reason: string): Observable<FinancialRecord> {
    return this.http.post<FinancialRecord>(`${this.baseUrl}/${id}/invoice-plan/${entryId}/skip`, { reason });
  }

  restorePlanEntry(id: string, entryId: string): Observable<FinancialRecord> {
    return this.http.post<FinancialRecord>(`${this.baseUrl}/${id}/invoice-plan/${entryId}/restore`, {});
  }

  addManualPlanEntry(id: string, claimDate: string): Observable<FinancialRecord> {
    return this.http.post<FinancialRecord>(`${this.baseUrl}/${id}/invoice-plan/manual`, { claimDate });
  }

  // Talks straight to InvoicesController (the new Invoice module), not
  // FinancialRecordsController — that's why these don't share `baseUrl`.
  getInvoicesForRecord(financialRecordId: string): Observable<FinanceInvoice[]> {
    return this.http.get<FinanceInvoice[]>(`${environment.apiClientUrl}/api/Invoices/by-financial-record/${financialRecordId}`);
  }

  sendInvoice(payload: SendFinanceInvoiceRequest): Observable<FinanceInvoice> {
    return this.http.post<FinanceInvoice>(`${environment.apiClientUrl}/api/Invoices/send`, payload);
  }

  resendInvoice(invoiceId: string, emailSubject: string, emailBody: string): Observable<FinanceInvoice> {
    return this.http.post<FinanceInvoice>(`${environment.apiClientUrl}/api/Invoices/${invoiceId}/resend`, { emailSubject, emailBody });
  }

  cancelInvoice(invoiceId: string, reason: string): Observable<FinanceInvoice> {
    return this.http.post<FinanceInvoice>(`${environment.apiClientUrl}/api/Invoices/${invoiceId}/cancel`, { reason });
  }

  sendCommunication(id: string, toEmail: string, recipientType: FinancialRecipientType, subject: string, body: string,
    templateKey?: string, relatedInvoiceId?: string): Observable<FinancialCommunication> {
    return this.http.post<FinancialCommunication>(`${this.baseUrl}/${id}/communications`, {
      toEmail, recipientType, subject, body, templateKey, relatedInvoiceId
    });
  }
}
