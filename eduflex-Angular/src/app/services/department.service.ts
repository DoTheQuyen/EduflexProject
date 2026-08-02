import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Department, CreateDepartmentRequest, DepartmentFilter, PagedResult } from '../models/department';

/**
 * Hand-written against DepartmentsController's REST routes directly, rather than the
 * NSwag-generated Client — the backend controller is brand new and NSwag hasn't been
 * regenerated yet, so the generated method names don't exist. Once `nswag run` has been
 * re-run against the updated backend, this can be migrated to call the generated Client
 * methods instead, matching the pattern already used for Enrolments.
 */
@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Departments`;

  constructor(private http: HttpClient) {}

  // Ungated directory listing — used by pickers (parent department dropdown, staff
  // assignment) as well as to resolve id/name for display elsewhere.
  getAll(): Observable<Department[]> {
    return this.http.get<Department[]>(this.baseUrl);
  }

  getById(id: string): Observable<Department> {
    return this.http.get<Department>(`${this.baseUrl}/${id}`);
  }

  search(filter: DepartmentFilter): Observable<PagedResult<Department>> {
    return this.http.post<PagedResult<Department>>(`${this.baseUrl}/search-departments`, filter);
  }

  create(department: CreateDepartmentRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, department);
  }

  update(id: string, department: CreateDepartmentRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, department);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
