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
  VisaStepKey
} from '../models/enrolment';

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

  addDocument(id: string, document: { fileName: string; category?: string; url: string; contentType?: string; sizeBytes: number }): Observable<EnrolmentDocument> {
    return this.http.post<EnrolmentDocument>(`${this.baseUrl}/${id}/documents`, document);
  }

  renameDocument(id: string, documentId: string, fileName: string): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/documents/${documentId}`, { fileName });
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
}
