import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  Task, CreateTaskRequest, UpdateTaskRequest, TaskFilter, ReassignTaskRequest, PagedResult
} from '../models/task';

/**
 * Hand-written against TaskItemsController's REST routes directly, rather than the
 * NSwag-generated Client — same reasoning as DepartmentService: the backend controller
 * is brand new and NSwag hasn't been regenerated yet. Once `nswag run` has been
 * re-run, this can be migrated to call the generated Client methods instead.
 */
@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Tasks`;

  constructor(private http: HttpClient) {}

  searchMyTasks(filter: TaskFilter): Observable<PagedResult<Task>> {
    return this.http.post<PagedResult<Task>>(`${this.baseUrl}/search-my-tasks`, filter);
  }

  searchAllTasks(filter: TaskFilter): Observable<PagedResult<Task>> {
    return this.http.post<PagedResult<Task>>(`${this.baseUrl}/search-all-tasks`, filter);
  }

  searchLinkedTasks(filter: TaskFilter): Observable<PagedResult<Task>> {
    return this.http.post<PagedResult<Task>>(`${this.baseUrl}/search-linked-tasks`, filter);
  }

  getById(id: string): Observable<Task> {
    return this.http.get<Task>(`${this.baseUrl}/${id}`);
  }

  create(task: CreateTaskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, task);
  }

  update(id: string, task: UpdateTaskRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}`, task);
  }

  addNote(id: string, content: string): Observable<Task> {
    return this.http.post<Task>(`${this.baseUrl}/${id}/notes`, { content });
  }

  reassign(id: string, request: ReassignTaskRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/reassign`, request);
  }

  changeStatus(id: string, status: 'Processing' | 'Completed'): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/${id}/status`, { status });
  }
}
