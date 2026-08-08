import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NotificationService } from '@services/notification.service';
import { environment } from '../../../../../environments/environment';
import { ApplicationDetail } from '../../../../../models/application';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-staff-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-detail.component.html',
  styleUrls: ['./application-detail.component.css']
})
export class ApplicationDetailComponent implements OnInit {
  application: ApplicationDetail | null = null;
  isLoading = false;

  private readonly applicationsUrl = `${environment.apiClientUrl}/api/Applications`;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.isLoading = true;
    this.http.get<ApplicationDetail>(`${this.applicationsUrl}/${id}/staff-view`).subscribe({
      next: (application) => {
        this.application = application;
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not load this application.'));
      }
    });
  }
}
