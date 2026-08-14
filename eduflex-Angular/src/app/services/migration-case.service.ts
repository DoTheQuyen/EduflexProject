import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  MigrationCase,
  CreateMigrationCaseRequest,
  UpdateCustomerInfoRequest,
  MigrationCaseFilterRequest,
  MigrationCaseCommunication,
  PagedResult
} from '../models/migration-case';
import { EnrolmentFormResponse, FormAnswer, FormResponseStatus } from '../models/dynamic-form';

/**
 * Hand-written against MigrationCasesController directly, rather than the
 * NSwag-generated Client — same reasoning as VisaProcessTemplateService: the backend
 * controller is brand new and NSwag hasn't been regenerated yet.
 */
@Injectable({ providedIn: 'root' })
export class MigrationCaseService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/MigrationCases`;

  constructor(private http: HttpClient) {}

  search(filter: MigrationCaseFilterRequest): Observable<PagedResult<MigrationCase>> {
    return this.http.post<PagedResult<MigrationCase>>(`${this.baseUrl}/search`, filter);
  }

  getById(id: string): Observable<MigrationCase> {
    return this.http.get<MigrationCase>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateMigrationCaseRequest): Observable<MigrationCase> {
    return this.http.post<MigrationCase>(this.baseUrl, request);
  }

  updateCustomerInfo(id: string, info: UpdateCustomerInfoRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/customer-info`, info);
  }

  saveStepDraft(id: string, stepKey: string, fields: Record<string, string>): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/steps/${stepKey}`, { fields });
  }

  completeStep(id: string, stepKey: string, fields: Record<string, string>): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/steps/${stepKey}/complete`, { fields });
  }

  reopenStep(id: string, stepKey: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/steps/${stepKey}/reopen`, {});
  }

  addDocument(id: string, document: {
    fileName: string; category?: string; note?: string; url: string; contentType?: string; sizeBytes: number;
  }): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/${id}/documents`, document);
  }

  deleteDocument(id: string, documentId: string): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}/documents/${documentId}`);
  }

  reassign(id: string, newOwnerUserId: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/reassign`, { newOwnerUserId });
  }

  sendCommunication(
    id: string,
    toEmail: string,
    recipientType: string,
    subject: string,
    body: string,
    attachedDocumentIds: string[],
    templateKey?: string
  ): Observable<MigrationCaseCommunication> {
    return this.http.post<MigrationCaseCommunication>(`${this.baseUrl}/${id}/communications`, {
      toEmail, recipientType, subject, body, attachedDocumentIds, templateKey
    });
  }

  // ----- Dynamic Forms — mirrors EnrolmentService's staff-side methods. No student-side
  // equivalent — see MigrationCaseService.SaveFormAnswersAsync's comment for why. -----

  requestForm(id: string, formTemplateId: string): Observable<EnrolmentFormResponse> {
    return this.http.post<EnrolmentFormResponse>(`${this.baseUrl}/${id}/forms/request`, { formTemplateId });
  }

  saveFormAnswers(id: string, responseId: string, answers: FormAnswer[], markSubmitted: boolean): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/forms/${responseId}/answers`, { answers, markSubmitted });
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

  exportForm(id: string, responseId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/forms/${responseId}/export`, { responseType: 'blob' });
  }
}
