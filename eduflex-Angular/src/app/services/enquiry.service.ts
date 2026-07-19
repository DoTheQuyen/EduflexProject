import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { CreateEnquiry, Enquiry } from '../models/enquiry';

@Injectable({
  providedIn: 'root'
})
export class EnquiryService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Enquiries`;

  constructor(private http: HttpClient) {}

  create(enquiry: CreateEnquiry): Observable<Enquiry> {
    return this.http.post<Enquiry>(this.baseUrl, enquiry);
  }
}
