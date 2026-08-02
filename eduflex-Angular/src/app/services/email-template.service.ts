import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { EmailTemplate } from '../models/enrolment';

/**
 * Hand-written against EmailTemplatesController directly, rather than the
 * NSwag-generated Client — same reasoning as DynamicFormTemplateService: the
 * controller changed shape (GetById/SetStatus added, Delete removed) and NSwag
 * hasn't been regenerated yet. Migrate to the generated Client once `nswag run`
 * has been re-run against the updated backend.
 */
@Injectable({ providedIn: 'root' })
export class EmailTemplateService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/EmailTemplates`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<EmailTemplate[]> {
    return this.http.get<EmailTemplate[]>(this.baseUrl);
  }

  getById(id: string): Observable<EmailTemplate> {
    return this.http.get<EmailTemplate>(`${this.baseUrl}/${id}`);
  }

  create(template: { key: string; name: string; subject: string; body: string }): Observable<EmailTemplate> {
    return this.http.post<EmailTemplate>(this.baseUrl, template);
  }

  update(id: string, template: { name: string; subject: string; body: string }): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, template);
  }

  setStatus(id: string, isActive: boolean): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}
