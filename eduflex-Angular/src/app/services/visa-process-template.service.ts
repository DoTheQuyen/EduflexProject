import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { VisaProcessTemplate, SaveVisaProcessTemplateRequest } from '../models/visa-process-template';

/**
 * Hand-written against VisaProcessTemplatesController directly, rather than the
 * NSwag-generated Client — same reasoning as DynamicFormTemplateService: the backend
 * controller is brand new and NSwag hasn't been regenerated yet. Migrate to the generated
 * Client once `nswag run` has been re-run against the updated backend.
 */
@Injectable({ providedIn: 'root' })
export class VisaProcessTemplateService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/VisaProcessTemplates`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<VisaProcessTemplate[]> {
    return this.http.get<VisaProcessTemplate[]>(this.baseUrl);
  }

  getById(id: string): Observable<VisaProcessTemplate> {
    return this.http.get<VisaProcessTemplate>(`${this.baseUrl}/${id}`);
  }

  create(template: SaveVisaProcessTemplateRequest): Observable<VisaProcessTemplate> {
    return this.http.post<VisaProcessTemplate>(this.baseUrl, template);
  }

  update(id: string, template: SaveVisaProcessTemplateRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, template);
  }

  setStatus(id: string, isActive: boolean): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}
