import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Client, ApplicationDto } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { formatDateTime } from '@app/shared/utils/date-time.util';

// A student may only hold one application that's still Pending or Approved —
// once it's Rejected (free to try elsewhere) or Studying (staff have actually
// enrolled them), a new application is allowed again.
const BLOCKS_NEW_APPLICATION_STATUSES = ['Pending', 'Approved'];

// Steps in the normal application lifecycle, in order — used to render a progress
// stepper for whichever application is currently active. 'Rejected' is a dead end so it
// isn't part of this happy-path sequence; the stepper simply isn't shown for it.
const APPLICATION_JOURNEY_STEPS = ['Pending', 'Approved', 'Studying'];
const APPLICATION_JOURNEY_LABELS: Record<string, string> = {
  Pending: 'Submitted',
  Approved: 'Approved',
  Studying: 'Enrolled',
};

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class StudentDashboardComponent implements OnInit {
  userInfo: any;
  today = new Date();
  isLoading = true;
  applications: ApplicationDto[] = [];

  constructor(
    private appService: Client,
    private authHelper: AuthHelperService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userInfo = this.authHelper.getCurrentUser();
    this.appService.applicationsGET(1, 100, undefined).subscribe({
      next: (result) => {
        this.applications = result.items ?? [];
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  get activeApplication(): ApplicationDto | undefined {
    return this.applications.find(a => BLOCKS_NEW_APPLICATION_STATUSES.includes(a.status ?? ''));
  }

  get hasActiveApplication(): boolean {
    return !!this.activeApplication;
  }

  get approvedCount(): number {
    return this.applications.filter(a => a.status === 'Approved').length;
  }

  get rejectedCount(): number {
    return this.applications.filter(a => a.status === 'Rejected').length;
  }

  get greeting(): string {
    const hour = this.today.getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  // Steps to render for the active application's progress stepper, each flagged with
  // whether it's already been reached. Empty when there's no active application or it's
  // off the happy path (e.g. Rejected), so the template can just *ngIf on the length.
  get journeySteps(): { label: string; reached: boolean; current: boolean }[] {
    const status = this.activeApplication?.status;
    if (!status || !APPLICATION_JOURNEY_STEPS.includes(status)) {
      return [];
    }
    const currentIndex = APPLICATION_JOURNEY_STEPS.indexOf(status);
    return APPLICATION_JOURNEY_STEPS.map((step, i) => ({
      label: APPLICATION_JOURNEY_LABELS[step],
      reached: i <= currentIndex,
      current: i === currentIndex,
    }));
  }

  get recentApplications(): ApplicationDto[] {
    return [...this.applications]
      .sort((a, b) => new Date(b.dateApplied ?? 0).getTime() - new Date(a.dateApplied ?? 0).getTime())
      .slice(0, 4);
  }

  formatDate(value: any): string {
    return formatDateTime(value, 'dd/MM/yyyy');
  }

  statusBadgeClass(status?: string): string {
    switch (status) {
      case 'Approved': return 'bg-success';
      case 'Rejected': return 'bg-danger';
      case 'Studying': return 'bg-primary';
      default: return 'bg-secondary';
    }
  }

  goToNewApplication(): void {
    if (this.hasActiveApplication) return;
    this.router.navigate(['/student-portal/application/new']);
  }
}
