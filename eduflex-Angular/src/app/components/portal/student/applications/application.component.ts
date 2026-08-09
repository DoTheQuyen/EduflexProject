import { Client, ApplicationDto } from '@services/api.services';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthHelperService } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { formatDateTime } from '@app/shared/utils/date-time.util';

// Once decided (Approved/Rejected) or actually enrolled (Studying), the
// application record itself is frozen — no more edits/withdraw/delete.
const EDIT_LOCKED_STATUSES = ['Approved', 'Rejected', 'Studying'];

// A student may only hold one application that's still Pending or Approved —
// once it's Rejected (free to try elsewhere) or Studying (staff have actually
// enrolled them), a new application is allowed again.
const BLOCKS_NEW_APPLICATION_STATUSES = ['Pending', 'Approved'];

@Component({
  selector: 'app-student-portal',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DataTableComponent],
  templateUrl: './application.component.html',
})
export class ApplicationComponent implements OnInit {
  userInfo: any;
  isLoading = true;
  applications: ApplicationDto[] = [];
  filteredApplications: ApplicationDto[] = [];

  searchTerm = '';
  statusFilter = 'all';

  get isStaffPortal(): boolean {
    return this.router.url.startsWith('/staff-portal');
  }

  columns: DataTableColumn<ApplicationDto>[] = [
    { field: 'applicationType', title: 'University' },
    { field: 'description', title: 'Course' },
    {
      field: 'dateApplied',
      title: 'Date Applied',
      className: 'text-center',
      formatter: (value) => formatDateTime(value, 'dd/MM/yyyy'),
    },
    { field: 'status', title: 'Status', className: 'text-center' },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<ApplicationDto>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
    {
      action: 'edit',
      label: 'Edit',
      icon: 'fa-pen',
      cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => !EDIT_LOCKED_STATUSES.includes(row.status ?? ''),
    },
    {
      action: 'delete',
      label: 'Delete',
      icon: 'fa-trash',
      cssClass: 'btn btn-sm btn-outline-danger',
      isVisible: (row) => !EDIT_LOCKED_STATUSES.includes(row.status ?? ''),
    },
  ];

  constructor(
    private router: Router,
    private appService: Client,
    private authHelper: AuthHelperService,
    private notification: NotificationService,
  ) {}

  ngOnInit(): void {
    this.userInfo = this.authHelper.getCurrentUser();
    this.loadStudentApplications();
  }

  loadStudentApplications(): void {
    // A student's own application list is inherently small (bounded by what one
    // person has applied to) and the search/filter below runs client-side across
    // the whole list, so we fetch one generous page rather than wiring real
    // prev/next paging into this screen.
    this.appService.applicationsGET(1, 100, undefined).subscribe({
      next: (result) => {
        this.applications = result.items ?? [];
        this.filteredApplications = [...this.applications];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading applications', err);
        this.isLoading = false;
      },
    });
  }

  get hasActiveApplication(): boolean {
    return this.applications.some((a) => BLOCKS_NEW_APPLICATION_STATUSES.includes(a.status ?? ''));
  }

  get inProgressApplicationsCount(): number {
    return this.applications.filter((a) => BLOCKS_NEW_APPLICATION_STATUSES.includes(a.status ?? ''))
      .length;
  }

  get studyingApplicationsCount(): number {
    return this.applications.filter((a) => a.status === 'Studying').length;
  }

  filterApplications(): void {
    let filtered = this.applications;

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(
        (app) =>
          app.description?.toLowerCase().includes(term) ||
          app.status?.toLowerCase().includes(term) ||
          app.applicationType?.toLowerCase().includes(term),
      );
    }

    if (this.statusFilter !== 'all') {
      filtered = filtered.filter((app) => app.status === this.statusFilter);
    }

    this.filteredApplications = filtered;
  }

  onSearchChange(event: any): void {
    this.searchTerm = event.target.value;
    this.filterApplications();
  }

  onStatusFilterChange(event: any): void {
    this.statusFilter = event.target.value;
    this.filterApplications();
  }

  onTableAction(event: DataTableAction<ApplicationDto>): void {
    const app = event.row;
    const base = this.isStaffPortal ? '/staff-portal/applications' : '/student-portal/application';
    if (event.action === 'view' || event.action === 'edit') {
      this.router.navigate([base, app.id]);
    } else if (event.action === 'delete') {
      this.notification.error(
        "Deleting an application isn't available yet — the backend doesn't support it.",
      );
    }
  }

  onNewApplication(): void {
    if (this.hasActiveApplication) return;
    this.router.navigate(['/student-portal/application/new']);
  }
}
