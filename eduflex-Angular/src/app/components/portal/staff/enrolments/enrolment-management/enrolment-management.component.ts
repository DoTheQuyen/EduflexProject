import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { EnrolmentService } from '@services/enrolment.service';
import { Enrolment, EnrolmentStatus, EnrolmentStatusOption, ENROLMENT_STATUS_LABELS, enrolmentStatusBadgeClass } from '../../../../../models/enrolment';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { formatDateTime } from '../../../../../shared/utils/date-time.util';

type OwnerScope = 'mine' | 'all';

@Component({
  selector: 'app-enrolment-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent, NotificationComponent],
  templateUrl: './enrolment-management.component.html',
  styleUrls: ['./enrolment-management.component.css'],
})
export class EnrolmentManagementComponent implements OnInit {
  enrolments: Enrolment[] = [];
  statusOptions: EnrolmentStatusOption[] = [];

  isLoading = false;
  pager = new TablePagerState();
  scope: OwnerScope = 'mine';

  permissions!: ModulePermissions;

  columns: DataTableColumn<Enrolment>[] = [
    {
      field: 'firstName',
      title: 'Student',
      render: (_value, row) => `${row.firstName ?? ''} ${row.lastName ?? ''}`.trim(),
    },
    { field: 'email', title: 'Email', hideOnLaptop: true },
    { field: 'mobile', title: 'Mobile', hideOnTablet: true },
    // Named 'statusBadge' rather than 'status' to avoid the data-table's built-in
    // status-column special-casing (hardcoded to Application-module status strings,
    // which fell back to a uniform gray badge for every Enrolment status) — renders
    // our own color-coded badge-pill markup instead, matching enrolment-detail's header pill.
    {
      field: 'statusBadge',
      title: 'Status',
      className: 'text-center',
      render: (_value, row: Enrolment) =>
        `<span class="badge-pill ${enrolmentStatusBadgeClass(row.status)}">${ENROLMENT_STATUS_LABELS[row.status]}</span>`,
    },
    { field: 'ownerName', title: 'Owner' },
    {
      field: 'createdAt',
      title: 'Created',
      className: 'text-center',
      formatter: (value) => (value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : ''),
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<Enrolment>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
  ];

  constructor(
    private enrolmentService: EnrolmentService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.permissions = this.authHelper.hasEnrolmentsPermission();
  }

  ngOnInit(): void {
    this.loadStatuses();
    this.loadEnrolments();
  }

  loadStatuses(): void {
    this.enrolmentService.getStatuses().subscribe({
      next: (statuses) => {
        this.statusOptions = statuses;
      },
      error: () => {},
    });
  }

  switchScope(scope: OwnerScope): void {
    if (this.scope === scope) return;
    this.scope = scope;
    this.pager.goToPage(1);
    this.loadEnrolments();
  }

  loadEnrolments(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view enrolments.');
      return;
    }

    this.isLoading = true;
    this.enrolmentService
      .search({
        pageNumber: this.pager.pageNumber,
        pageSize: this.pager.pageSize,
        searchTerm: this.pager.searchTerm || undefined,
        mineOnly: this.scope === 'mine',
      })
      .subscribe({
        next: (result) => {
          this.enrolments = result.items ?? [];
          this.pager.totalCount = result.totalCount ?? 0;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        },
      });
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadEnrolments();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadEnrolments();
  }

  onRefresh(): void {
    this.pager.search('');
    this.loadEnrolments();
  }

  onTableAction(event: DataTableAction<Enrolment>): void {
    if (event.action === 'view') {
      this.router.navigate(['/staff-portal/enrolments', event.row.id]);
    }
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/enrolments/new']);
  }
}
