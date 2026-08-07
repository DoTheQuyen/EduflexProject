import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Client, StudentAccountDto, StudentFilterDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-student-management',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  templateUrl: './student-management.component.html',
  styleUrls: ['./student-management.component.css']
})
export class StudentManagementComponent implements OnInit {
  students: StudentAccountDto[] = [];

  isLoading = false;

  pager = new TablePagerState();
  activeFilter = 'all';

  permissions!: ModulePermissions;

  columns: DataTableColumn<StudentAccountDto>[] = [
    { field: 'email', title: 'Email' },
    { field: 'firstName', title: 'First Name' },
    { field: 'lastName', title: 'Last Name' },
    { field: 'mobile', title: 'Mobile' },
    { field: 'passportNumber', title: 'Passport No.' },
    { field: 'isActive', title: 'Active', formatter: (value) => value ? 'Yes' : 'No' },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

  rowActions: DataTableRowAction<StudentAccountDto>[] = [];

  constructor(
    private router: Router,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasStudentsPermission();

    this.rowActions = [
      ...(this.permissions.view ? [{ action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' }] : []),
      ...(this.permissions.edit ? [{ action: 'reactivate', label: 'Reactivate', icon: 'fa-user-check', cssClass: 'btn btn-sm btn-outline-success', isVisible: (row: StudentAccountDto) => !row.isActive }] : []),
      ...(this.permissions.edit ? [{ action: 'reset-password', label: 'Send Reset Password', icon: 'fa-key', cssClass: 'btn btn-sm btn-outline-secondary' }] : [])
    ];
  }

  ngOnInit(): void {
    this.loadStudents();
  }

  loadStudents(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view students.');
      return;
    }

    this.isLoading = true;
    const isActive = this.activeFilter === 'all' ? undefined : this.activeFilter === 'true';
    const filter = new StudentFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      isActive
    });
    this.apiClient.searchStudents(filter).subscribe({
      next: (result) => {
        this.students = result.items ?? [];
        this.pager.totalCount = result.totalCount ?? 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadStudents();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadStudents();
  }

  onActiveFilterChange(event: Event): void {
    this.activeFilter = (event.target as HTMLSelectElement).value;
    this.pager.goToPage(1);
    this.loadStudents();
  }

  onTableAction(event: DataTableAction<StudentAccountDto>): void {
    switch (event.action) {
      case 'view':
        this.router.navigate(['/staff-portal/students', event.row.id]);
        break;
      case 'reactivate':
        this.reactivateStudent(event.row);
        break;
      case 'reset-password':
        this.sendResetPassword(event.row);
        break;
    }
  }

  private reactivateStudent(row: StudentAccountDto): void {
    if (!row.id) return;
    if (!window.confirm(`Reactivate ${row.firstName} ${row.lastName}?`)) return;

    this.apiClient.reactivate(row.id).subscribe({
      next: () => {
        this.loadStudents();
        this.notificationService.success('Student reactivated.');
      },
      error: (err) => {
        this.notificationService.error(extractApiErrorMessage(err, 'Could not reactivate this student.'));
      }
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
        this.notificationService.error(extractApiErrorMessage(err, 'Could not send the password reset email.'));
      }
    });
  }
}
