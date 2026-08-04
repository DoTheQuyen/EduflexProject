import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { DynamicFormTemplate, SaveDynamicFormTemplateRequest } from '../models/dynamic-form';

/**
 * Hand-written against DynamicFormTemplatesController directly, rather than the
 * NSwag-generated Client — same reasoning as EnrolmentService: the backend controller
 * is brand new and NSwag hasn't been regenerated yet. Migrate to the generated Client
 * once `nswag run` has been re-run against the updated backend.
 */
@Injectable({ providedIn: 'root' })
export class DynamicFormTemplateService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/DynamicFormTemplates`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DynamicFormTemplate[]> {
    return this.http.get<DynamicFormTemplate[]>(this.baseUrl);
  }

  getById(id: string): Observable<DynamicFormTemplate> {
    return this.http.get<DynamicFormTemplate>(`${this.baseUrl}/${id}`);
  }

  create(template: SaveDynamicFormTemplateRequest): Observable<DynamicFormTemplate> {
    return this.http.post<DynamicFormTemplate>(this.baseUrl, template);
  }

  update(id: string, template: SaveDynamicFormTemplateRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, template);
  }

  setStatus(id: string, isActive: boolean): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}
