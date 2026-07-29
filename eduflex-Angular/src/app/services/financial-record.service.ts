import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  CommissionAdjustment, CreateInvoiceDraftRequest, FinancialCommunication, FinancialRecipientType, FinancialRecord,
  FinancialRecordFilter, Invoice
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

  createInvoiceDraft(id: string, payload: CreateInvoiceDraftRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/invoices`, payload);
  }

  updateInvoiceDraft(id: string, invoiceId: string, htmlContent: string, periodTotal: number): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/invoices/${invoiceId}`, { htmlContent, periodTotal });
  }

  generateInvoicePdf(id: string, invoiceId: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/invoices/${invoiceId}/generate-pdf`, {});
  }

  getInvoiceDownloadLink(id: string, invoiceId: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${this.baseUrl}/${id}/invoices/${invoiceId}/download-link`);
  }

  sendCommunication(id: string, toEmail: string, recipientType: FinancialRecipientType, subject: string, body: string,
    templateKey?: string, relatedInvoiceId?: string): Observable<FinancialCommunication> {
    return this.http.post<FinancialCommunication>(`${this.baseUrl}/${id}/communications`, {
      toEmail, recipientType, subject, body, templateKey, relatedInvoiceId
    });
  }
}
