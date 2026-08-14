import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { PractitionerTag, SavePractitionerTagRequest } from '../models/practitioner-tag';

/**
 * Hand-written against PractitionerTagsController directly, rather than the
 * NSwag-generated Client — same reasoning as VisaProcessTemplateService.
 */
@Injectable({ providedIn: 'root' })
export class PractitionerTagService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/PractitionerTags`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<PractitionerTag[]> {
    return this.http.get<PractitionerTag[]>(this.baseUrl);
  }

  create(tag: SavePractitionerTagRequest): Observable<PractitionerTag> {
    return this.http.post<PractitionerTag>(this.baseUrl, tag);
  }

  update(id: string, tag: SavePractitionerTagRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, tag);
  }

  setActive(id: string, isActive: boolean): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}
