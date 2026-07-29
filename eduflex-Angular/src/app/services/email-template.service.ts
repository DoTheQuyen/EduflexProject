import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { EmailTemplate } from '../models/enrolment';

@Injectable({ providedIn: 'root' })
export class EmailTemplateService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/EmailTemplates`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<EmailTemplate[]> {
    return this.http.get<EmailTemplate[]>(this.baseUrl);
  }

  create(template: { key: string; name: string; subject: string; body: string }): Observable<EmailTemplate> {
    return this.http.post<EmailTemplate>(this.baseUrl, template);
  }

  update(id: string, template: { name: string; subject: string; body: string }): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, template);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
