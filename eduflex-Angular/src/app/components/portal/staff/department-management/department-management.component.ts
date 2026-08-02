import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, UserSummaryDto, UserFilterDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DepartmentService } from '@services/department.service';
import { Department, CreateDepartmentRequest } from '../../../../models/department';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-department-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTableComponent, ModalComponent, NotificationComponent],
  templateUrl: './department-management.component.html',
  styleUrls: ['./department-management.component.css']
})
export class DepartmentManagementComponent implements OnInit {
  departments: Department[] = [];
  allDepartments: Department[] = [];
  allStaff: UserSummaryDto[] = [];

  selectedMemberIds: string[] = [];
  selectedHeadId = '';

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();

  permissions!: ModulePermissions;

  departmentForm: FormGroup;

  columns: DataTableColumn<Department>[] = [
    { field: 'name', title: 'Name' },
    { field: 'description', title: 'Description' },
    { field: 'parentDepartmentId', title: 'Parent', formatter: (value) => this.getDepartmentName(value) },
    { field: 'headUserId', title: 'Head', formatter: (value) => value ? this.getStaffName(value) : '—' },
    { field: 'memberUserIds', title: 'Members', formatter: (value) => (value?.length ?? 0) + ' member(s)' },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

  rowActions: DataTableRowAction<Department>[] = [];

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private departmentService: DepartmentService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.departmentForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', [Validators.maxLength(300)]],
      parentDepartmentId: ['']
    });

    this.permissions = this.authHelper.hasDepartmentsPermission();

    // Delete lives inside the edit modal (with its own confirm-before-delete step),
    // not as a separate row action — one less place to accidentally click delete from.
    this.rowActions = [
      ...(this.permissions.edit ? [{ action: 'edit', label: 'Edit', icon: 'fa-edit', cssClass: 'btn btn-sm btn-outline-primary' }] : [])
    ];
  }

  ngOnInit(): void {
    this.loadDepartments();
    this.loadAllDepartments();
    this.loadStaff();
  }

  loadDepartments(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view departments.');
      return;
    }

    this.isLoading = true;
    this.departmentService.search({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined
    }).subscribe({
      next: (result) => {
        this.departments = result.items ?? [];
        this.pager.totalCount = result.totalCount ?? 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  // Ungated directory — powers the parent-department dropdown and the Name/Parent
  // lookups in the list, independent of the paginated/gated search above.
  loadAllDepartments(): void {
    this.departmentService.getAll().subscribe({
      next: (departments) => { this.allDepartments = departments ?? []; },
      error: () => {}
    });
  }

  loadStaff(): void {
    // Powers the member-assignment checklist — the server's max page size (100) is
    // effectively "all staff" for a picker this size, same reasoning as the role
    // dropdown in user-management.
    this.apiClient.searchUsers(new UserFilterDto({ pageNumber: 1, pageSize: 100, isActive: true })).subscribe({
      next: (result) => { this.allStaff = (result.items ?? []).filter(u => u.roleName !== 'Student'); },
      error: () => {}
    });
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadDepartments();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadDepartments();
  }

  getDepartmentName(id?: string): string {
    if (!id) return '—';
    return this.allDepartments.find(d => d.id === id)?.name ?? '—';
  }

  // TODO(department-migration): drop the `as any` once `nswag run` has regenerated
  // api.services.ts with MiddleName on UserSummaryDto.
  getStaffName(id?: string): string {
    if (!id) return '—';
    const staff = this.allStaff.find(u => u.id === id);
    if (!staff) return '—';
    const middleName = (staff as any).middleName as string | undefined;
    const middle = middleName ? `${middleName} ` : '';
    return `${staff.firstName} ${middle}${staff.lastName} - Role: ${staff.roleName}`;
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.departmentForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  isMemberSelected(userId: string | undefined): boolean {
    return !!userId && this.selectedMemberIds.includes(userId);
  }

  toggleMember(userId: string | undefined, checked: boolean): void {
    if (!userId) return;
    if (checked) {
      if (!this.selectedMemberIds.includes(userId)) {
        this.selectedMemberIds.push(userId);
      }
    } else {
      this.selectedMemberIds = this.selectedMemberIds.filter(id => id !== userId);
      // The head must also be a member — dropping a selected head from the member
      // list clears the head too, rather than leaving a dangling, invalid selection.
      if (this.selectedHeadId === userId) {
        this.selectedHeadId = '';
      }
    }
  }

  openModal(): void {
    this.editingId = null;
    this.departmentForm.reset();
    this.selectedMemberIds = [];
    this.selectedHeadId = '';
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  openEditModal(department: Department): void {
    this.editingId = department.id;
    this.departmentForm.reset({
      name: department.name,
      description: department.description,
      parentDepartmentId: department.parentDepartmentId ?? ''
    });
    this.selectedMemberIds = [...(department.memberUserIds ?? [])];
    this.selectedHeadId = department.headUserId ?? '';
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    const requiredPermission = this.editingId ? this.permissions.edit : this.permissions.add;
    if (!requiredPermission) {
      this.notificationService.error(this.editingId ? 'You do not have permission to edit departments.' : 'You do not have permission to add departments.');
      return;
    }

    this.departmentForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.departmentForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const formValue = this.departmentForm.value;
    const payload: CreateDepartmentRequest = {
      name: formValue.name,
      description: formValue.description || undefined,
      parentDepartmentId: formValue.parentDepartmentId || undefined,
      headUserId: this.selectedHeadId || undefined,
      memberUserIds: this.selectedMemberIds
    };

    if (this.editingId) {
      this.departmentService.update(this.editingId, payload).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.closeModal();
          this.loadDepartments();
          this.loadAllDepartments();
          this.notificationService.success('Department updated successfully.');
        },
        error: (err) => {
          this.isSubmitting = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the department. Please try again.');
          this.notificationService.error(this.errorMessage);
        }
      });
    } else {
      this.departmentService.create(payload).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.closeModal();
          this.loadDepartments();
          this.loadAllDepartments();
          this.notificationService.success('Department created successfully.');
        },
        error: (err) => {
          this.isSubmitting = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the department. Please try again.');
          this.notificationService.error(this.errorMessage);
        }
      });
    }
  }

  onDelete(): void {
    if (!this.editingId || !this.permissions.delete) return;

    this.isSubmitting = true;
    this.departmentService.delete(this.editingId).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadDepartments();
        this.loadAllDepartments();
        this.notificationService.success('Department deleted successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Could not delete this department. Please try again.');
        this.notificationService.error(this.errorMessage);
      }
    });
  }

  onTableAction(event: DataTableAction<Department>): void {
    if (event.action === 'edit') {
      this.openEditModal(event.row);
    }
  }
}
