import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, StudentAccountDto, UpdateStudentDto, StudentFilterDto, AddressDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-student-management',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, DataTableComponent, ModalComponent, NotificationComponent],
  templateUrl: './student-management.component.html',
  styleUrls: ['./student-management.component.css']
})
export class StudentManagementComponent implements OnInit {
  students: StudentAccountDto[] = [];

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();
  activeFilter = 'all';

  permissions!: ModulePermissions;

  studentForm: FormGroup;

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

  constructor(private fb: FormBuilder, private apiClient: Client, private authHelper: AuthHelperService, private notificationService: NotificationService) {
    this.studentForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      mobile: ['', [Validators.required, Validators.maxLength(20)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      nationality: ['', [Validators.required, Validators.maxLength(50)]],
      passportNumber: ['', [Validators.required, Validators.maxLength(20)]],
      dateOfBirth: ['', [Validators.required]],
      street: ['', Validators.required],
      suburb: [''],
      city: ['', Validators.required],
      state: [''],
      country: ['', Validators.required],
      postalCode: ['', Validators.required]
    });

    this.permissions = this.authHelper.hasStudentsPermission();

    this.rowActions = [
      ...(this.permissions.edit ? [{ action: 'edit', label: 'Edit', icon: 'fa-edit', cssClass: 'btn btn-sm btn-outline-primary' }] : []),
      ...(this.permissions.delete ? [{ action: 'deactivate', label: 'Deactivate', icon: 'fa-user-slash', cssClass: 'btn btn-sm btn-outline-danger', isVisible: (row: StudentAccountDto) => !!row.isActive }] : []),
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

  isFieldInvalid(fieldName: string): boolean {
    const control = this.studentForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openEditModal(student: StudentAccountDto): void {
    this.editingId = student.id ?? null;
    this.studentForm.reset({
      email: student.email,
      mobile: student.mobile,
      firstName: student.firstName,
      lastName: student.lastName,
      nationality: student.nationality,
      passportNumber: student.passportNumber,
      dateOfBirth: this.toDateInputValue(student.dateOfBirth),
      street: student.address?.street,
      suburb: student.address?.suburb,
      city: student.address?.city,
      state: student.address?.state,
      country: student.address?.country,
      postalCode: student.address?.postalCode
    });
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  private toDateInputValue(date?: Date): string {
    if (!date) return '';
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private buildAddress(): AddressDto {
    const v = this.studentForm.value;
    return new AddressDto({
      street: v.street,
      suburb: v.suburb || undefined,
      city: v.city,
      state: v.state || undefined,
      country: v.country,
      postalCode: v.postalCode
    });
  }

  onSubmit(): void {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit students.');
      return;
    }

    this.studentForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.studentForm.invalid) {
      return;
    }

    this.submitUpdate();
  }

  private submitUpdate(): void {
    this.isSubmitting = true;
    const v = this.studentForm.value;
    const updateDto = new UpdateStudentDto({
      email: v.email,
      mobile: v.mobile,
      firstName: v.firstName,
      lastName: v.lastName,
      nationality: v.nationality,
      passportNumber: v.passportNumber,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      address: this.buildAddress()
    });

    this.apiClient.studentsPUT(this.editingId!, updateDto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadStudents();
        this.notificationService.success('Student updated successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong updating this student. Please try again.');
      }
    });
  }

  onTableAction(event: DataTableAction<StudentAccountDto>): void {
    switch (event.action) {
      case 'edit':
        this.openEditModal(event.row);
        break;
      case 'deactivate':
        this.deactivateStudent(event.row);
        break;
      case 'reactivate':
        this.reactivateStudent(event.row);
        break;
      case 'reset-password':
        this.sendResetPassword(event.row);
        break;
    }
  }

  private deactivateStudent(row: StudentAccountDto): void {
    if (!row.id) return;
    if (!window.confirm(`Deactivate ${row.firstName} ${row.lastName}? They will no longer be able to log in.`)) return;

    this.apiClient.deactivate(row.id).subscribe({
      next: () => {
        this.loadStudents();
        this.notificationService.success('Student deactivated.');
      },
      error: (err) => {
        this.notificationService.error(extractApiErrorMessage(err, 'Could not deactivate this student.'));
      }
    });
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
