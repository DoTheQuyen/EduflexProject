import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Client, PersonType, StudentAccountDto, StudentFilterDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

export type ContactTab = 'student' | 'customer';

interface TabState {
  rows: StudentAccountDto[];
  pager: TablePagerState;
  activeFilter: string;
  loaded: boolean;
}

@Component({
  selector: 'app-student-management',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  templateUrl: './student-management.component.html',
  styleUrls: ['./student-management.component.css'],
})
export class StudentManagementComponent implements OnInit {
  // Students and Customers are the same underlying record (see StudentModel.Type) —
  // migration-case-only contacts just aren't enrolled in a course. Same table/columns/
  // actions for both, so they live on one page as tabs (each loaded once, lazily,
  // same pattern as AccountsComponent) rather than as two separate menu entries.
  activeTab: ContactTab = 'student';

  tabs: Record<ContactTab, TabState> = {
    student: { rows: [], pager: new TablePagerState(), activeFilter: 'all', loaded: false },
    customer: { rows: [], pager: new TablePagerState(), activeFilter: 'all', loaded: false },
  };

  isLoading = false;

  permissions!: ModulePermissions;

  columns: DataTableColumn<StudentAccountDto>[] = [
    { field: 'email', title: 'Email', hideOnTablet: true },
    { field: 'firstName', title: 'First Name', className: 'text-center' },
    { field: 'lastName', title: 'Last Name', className: 'text-center' },
    { field: 'mobile', title: 'Mobile' },
    { field: 'passportNumber', title: 'Passport No.', hideOnLaptop: true },
    {
      field: 'isActive',
      title: 'Active',
      className: 'text-center',
      formatter: (value) => (value ? 'Yes' : 'No'),
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<StudentAccountDto>[] = [];

  constructor(
    private router: Router,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
  ) {
    this.permissions = this.authHelper.hasStudentsPermission();

    this.rowActions = [
      ...(this.permissions.view
        ? [
            {
              action: 'view',
              label: 'View',
              icon: 'fa-eye',
              cssClass: 'btn btn-sm btn-outline-primary',
            },
          ]
        : []),
      ...(this.permissions.edit
        ? [
            {
              action: 'reactivate',
              label: 'Reactivate',
              icon: 'fa-user-check',
              cssClass: 'btn btn-sm btn-outline-success',
              isVisible: (row: StudentAccountDto) => !row.isActive,
            },
          ]
        : []),
      ...(this.permissions.edit
        ? [
            {
              action: 'reset-password',
              label: 'Send Reset Password',
              icon: 'fa-key',
              cssClass: 'btn btn-sm btn-outline-secondary',
            },
          ]
        : []),
    ];
  }

  get current(): TabState {
    return this.tabs[this.activeTab];
  }

  private get personType(): PersonType {
    return this.activeTab === 'student' ? PersonType.Student : PersonType.Customer;
  }

  ngOnInit(): void {
    this.load();
  }

  switchTab(tab: ContactTab): void {
    this.activeTab = tab;
    if (!this.current.loaded) this.load();
  }

  load(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view contacts.');
      return;
    }

    const tab = this.current;
    this.isLoading = true;
    const isActive = tab.activeFilter === 'all' ? undefined : tab.activeFilter === 'true';
    const filter = new StudentFilterDto({
      pageNumber: tab.pager.pageNumber,
      pageSize: tab.pager.pageSize,
      searchTerm: tab.pager.searchTerm || undefined,
      isActive,
      type: this.personType,
    });
    this.apiClient.searchStudents(filter).subscribe({
      next: (result) => {
        tab.rows = result.items ?? [];
        tab.pager.totalCount = result.totalCount ?? 0;
        tab.loaded = true;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  onPageChange(page: number): void {
    this.current.pager.goToPage(page);
    this.load();
  }

  onSearchChange(term: string): void {
    this.current.pager.search(term);
    this.load();
  }

  onActiveFilterChange(event: Event): void {
    this.current.activeFilter = (event.target as HTMLSelectElement).value;
    this.current.pager.goToPage(1);
    this.load();
  }

  onRefresh(): void {
    this.current.activeFilter = 'all';
    this.current.pager.search('');
    this.load();
  }

  onTableAction(event: DataTableAction<StudentAccountDto>): void {
    switch (event.action) {
      case 'view':
        this.router.navigate(['/staff-portal/contacts', event.row.id]);
        break;
      case 'reactivate':
        this.reactivateStudent(event.row);
        break;
      case 'reset-password':
        this.sendResetPassword(event.row);
        break;
    }
  }

  addRoute(): string[] {
    return ['/staff-portal/contacts/new'];
  }

  addQueryParams(): { type: ContactTab } {
    return { type: this.activeTab };
  }

  private reactivateStudent(row: StudentAccountDto): void {
    if (!row.id) return;
    if (!window.confirm(`Reactivate ${row.firstName} ${row.lastName}?`)) return;

    this.apiClient.reactivate(row.id).subscribe({
      next: () => {
        this.load();
        this.notificationService.success('Reactivated.');
      },
      error: (err) => {
        this.notificationService.error(
          extractApiErrorMessage(err, 'Could not reactivate this record.'),
        );
      },
    });
  }

  private sendResetPassword(row: StudentAccountDto): void {
    if (!row.id) return;
    if (!window.confirm(`Send a password reset email to ${row.email}?`)) return;

    this.apiClient.sendResetPassword(row.id).subscribe({
      next: () => {
        this.notificationService.success('Password reset email sent.');
      },
      error: (err) => {
        this.notificationService.error(
          extractApiErrorMessage(err, 'Could not send the password reset email.'),
        );
      },
    });
  }
}
