import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { CreateFeedback, Feedback } from '../models/feedback';

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Feedbacks`;

  constructor(private http: HttpClient) {}

  getLatest(count: number = 10): Observable<Feedback[]> {
    return this.http.get<Feedback[]>(`${this.baseUrl}/latest?count=${count}`);
  }

  getAll(): Observable<Feedback[]> {
    return this.http.get<Feedback[]>(this.baseUrl);
  }

  create(feedback: CreateFeedback): Observable<Feedback> {
    return this.http.post<Feedback>(this.baseUrl, feedback);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}