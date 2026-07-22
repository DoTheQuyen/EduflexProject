
// keep this for study
// import { Injectable } from '@angular/core';
// import { HttpClient } from '@angular/common/http';
// import { Observable } from 'rxjs';
// import { environment } from '../environments/environment';
// import { CoursePromotion, CreateCoursePromotion } from '../models/course-promotion';

// @Injectable({
//   providedIn: 'root'
// })
// export class CoursePromotionService {
//   private readonly baseUrl = `${environment.apiClientUrl}/api/CoursePromotions`;

//   constructor(private http: HttpClient) {}

//   getLatest(count: number = 10): Observable<CoursePromotion[]> {
//     return this.http.get<CoursePromotion[]>(`${this.baseUrl}/latest?count=${count}`);
//   }

//   getAll(): Observable<CoursePromotion[]> {
//     return this.http.get<CoursePromotion[]>(this.baseUrl);
//   }

//   create(promotion: CreateCoursePromotion): Observable<CoursePromotion> {
//     return this.http.post<CoursePromotion>(this.baseUrl, promotion);
//   }

//   update(id: string, promotion: CreateCoursePromotion): Observable<CoursePromotion> {
//     return this.http.put<CoursePromotion>(`${this.baseUrl}/${id}`, promotion);
//   }

//   delete(id: string): Observable<void> {
//     return this.http.delete<void>(`${this.baseUrl}/${id}`);
//   }
// }
