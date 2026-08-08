import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  Enrolment,
  CreateEnrolmentRequest,
  EnrolmentFilter,
  EnrolmentStatusOption,
  PagedResult,
  EnrolmentDocument,
  EnrolmentCommunication,
  RecipientType,
  VisaStepKey,
  MyEnrolmentSummary
} from '../models/enrolment';
import { EnrolmentFormResponse, FormAnswer, FormResponseStatus, MyEnrolmentForms } from '../models/dynamic-form';

/**
 * Hand-written against the new EnrolmentsController REST routes directly, rather than the
 * NSwag-generated Client — the backend controller is brand new and NSwag hasn't been regenerated
 * yet, so the generated method names don't exist. Once `nswag run` has been re-run against the
 * updated backend, this can be migrated to call the generated Client methods instead, matching
 * the pattern already used for Course Promotions / Education Partners.
 */
@Injectable({ providedIn: 'root' })
export class EnrolmentService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Enrolments`;

  constructor(private http: HttpClient) {}

  createFromEnquiry(enquiryId: string, request: CreateEnrolmentRequest): Observable<Enrolment> {
    return this.http.post<Enrolment>(`${this.baseUrl}/from-enquiry/${enquiryId}`, request);
  }

  createIndependent(request: CreateEnrolmentRequest): Observable<Enrolment> {
    return this.http.post<Enrolment>(this.baseUrl, request);
  }

  get(id: string): Observable<Enrolment> {
    return this.http.get<Enrolment>(`${this.baseUrl}/${id}`);
  }

  getByStudent(studentUserId: string): Observable<Enrolment[]> {
    return this.http.get<Enrolment[]>(`${this.baseUrl}/by-student/${studentUserId}`);
  }

  getStatuses(): Observable<EnrolmentStatusOption[]> {
    return this.http.get<EnrolmentStatusOption[]>(`${this.baseUrl}/enrolment-statuses`);
  }

  search(filter: EnrolmentFilter): Observable<PagedResult<Enrolment>> {
    return this.http.post<PagedResult<Enrolment>>(`${this.baseUrl}/search-enrolments`, filter);
  }

  update(id: string, enrolment: Enrolment): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, enrolment);
  }

  reassign(id: string, newOwnerUserId: string): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/reassign`, { newOwnerUserId });
  }

  addDocument(id: string, document: { fileName: string; category?: string; courseApplicationId?: string; note?: string; url: string; contentType?: string; sizeBytes: number }): Observable<EnrolmentDocument> {
    return this.http.post<EnrolmentDocument>(`${this.baseUrl}/${id}/documents`, document);
  }

  renameDocument(id: string, documentId: string, fileName: string, note?: string): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/documents/${documentId}`, { fileName, note });
  }

  deleteDocument(id: string, documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/documents/${documentId}`);
  }

  sendCommunication(
    id: string,
    toEmail: string,
    recipientType: RecipientType,
    subject: string,
    body: string,
    attachedDocumentIds: string[],
    templateKey?: string
  ): Observable<EnrolmentCommunication> {
    return this.http.post<EnrolmentCommunication>(`${this.baseUrl}/${id}/communications`, {
      toEmail, recipientType, subject, body, attachedDocumentIds, templateKey
    });
  }

  saveVisaStepDraft(id: string, stepKey: VisaStepKey, fields: Record<string, string>): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/visa-steps/${stepKey}`, { fields });
  }

  completeVisaStep(id: string, stepKey: VisaStepKey, fields: Record<string, string>): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/visa-steps/${stepKey}/complete`, { fields });
  }

  reopenVisaStep(id: string, stepKey: VisaStepKey): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/visa-steps/${stepKey}/reopen`, {});
  }

  finalize(id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/finalize`, {});
  }

  // ----- Dynamic Forms — staff -----

  requestForm(id: string, formTemplateId: string): Observable<EnrolmentFormResponse> {
    return this.http.post<EnrolmentFormResponse>(`${this.baseUrl}/${id}/forms/request`, { formTemplateId });
  }

  withdrawFormRequest(id: string, responseId: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/withdraw`, {});
  }

  archiveFormResponse(id: string, responseId: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/archive`, {});
  }

  setFormResponseStatus(id: string, responseId: string, status: FormResponseStatus): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/status`, { status });
  }

  reopenFormForEdit(id: string, responseId: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/reopen`, {});
  }

  staffEditFormResponse(id: string, responseId: string, answers: FormAnswer[]): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/answers`, { answers });
  }

  // Backend streams the PDF bytes directly (with its own Content-Disposition filename)
  // rather than a blob storage URL — see EnrolmentsController.ExportForm. Fetched as a
  // Blob so the caller can trigger a download with an explicit filename via a local
  // blob: object URL, which is honored reliably regardless of the origin server's headers.
  exportForm(id: string, responseId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/forms/${responseId}/export`, { responseType: 'blob' });
  }

  // ----- Dynamic Forms — student -----

  getMyForms(applicationId: string): Observable<MyEnrolmentForms> {
    return this.http.get<MyEnrolmentForms>(`${this.baseUrl}/by-application/${applicationId}/forms`);
  }

  getMySummary(applicationId: string): Observable<MyEnrolmentSummary> {
    return this.http.get<MyEnrolmentSummary>(`${this.baseUrl}/by-application/${applicationId}/summary`);
  }

  saveFormDraft(id: string, responseId: string, answers: FormAnswer[]): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/draft`, { answers });
  }

  submitForm(id: string, responseId: string, answers: FormAnswer[]): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/submit`, { answers });
  }
}
