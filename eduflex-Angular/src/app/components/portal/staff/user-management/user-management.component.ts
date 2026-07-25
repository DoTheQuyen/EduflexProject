import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, UserSummaryDto, CreateUserDto, UserDto, RoleDto, UserFilterDto, RoleFilterDto } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { PermissionKeys } from '../../../../shared/constants/permission-keys';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTableComponent, ModalComponent, NotificationComponent],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  users: UserSummaryDto[] = [];
  roles: RoleDto[] = [];

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();
  roleFilter = '';
  activeFilter = 'all';

  canView = false;
  canAdd = false;
  canEdit = false;

  userForm: FormGroup;

  columns: DataTableColumn<UserSummaryDto>[] = [
    { field: 'email', title: 'Email' },
    { field: 'firstName', title: 'First Name' },
    { field: 'lastName', title: 'Last Name' },
    { field: 'roleName', title: 'Role' },
    { field: 'isActive', title: 'Active', formatter: (value) => value ? 'Yes' : 'No' },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

  rowActions: DataTableRowAction<UserSummaryDto>[] = [];

  constructor(private fb: FormBuilder, private apiClient: Client, private authHelper: AuthHelperService, private notificationService: NotificationService) {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      roleId: ['', [Validators.required]],
      isActive: [true]
    });

this.canAdd = this.authHelper.hasPermission(PermissionKeys.UsersAdd);
this.canEdit = this.authHelper.hasPermission(PermissionKeys.UsersEdit);

    this.rowActions = [
      ...(this.canEdit ? [{ action: 'edit', label: 'Edit', icon: 'fa-edit', cssClass: 'btn btn-sm btn-outline-primary' }] : [])
    ];
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers(): void {
    this.isLoading = true;
    const isActive = this.activeFilter === 'all' ? undefined : this.activeFilter === 'true';
    const filter = new UserFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      roleId: this.roleFilter || undefined,
      isActive
    });
    this.apiClient.searchUsers(filter).subscribe({
      next: (result) => {
        this.users = result.items ?? [];
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
    this.loadUsers();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadUsers();
  }

  onRoleFilterChange(event: Event): void {
    this.roleFilter = (event.target as HTMLSelectElement).value;
    this.pager.goToPage(1);
    this.loadUsers();
  }

  onActiveFilterChange(event: Event): void {
    this.activeFilter = (event.target as HTMLSelectElement).value;
    this.pager.goToPage(1);
    this.loadUsers();
  }

  loadRoles(): void {
    // Roles power the dropdown below, not a paginated list view — fetching the
    // server's max page size (100) is effectively "all roles" for a reference
    // table this small, without needing a separate unpaginated endpoint.
    this.apiClient.searchRoles(new RoleFilterDto({ pageNumber: 1, pageSize: 100 })).subscribe({
      next: (result) => {
        this.roles = result.items ?? [];
      },
      error: () => {}
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.userForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openModal(): void {
     this.editingId = null;
    this.userForm.reset({ isActive: true });
    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  openEditModal(user: UserSummaryDto): void {
      this.editingId = user.id ?? null;
    this.userForm.reset({
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      roleId: user.roleId,
      isActive: user.isActive
    });
    this.userForm.get('password')?.clearValidators();
    this.userForm.get('password')?.updateValueAndValidity();
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    this.userForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.userForm.invalid) {
      return;
    }

    this.isSubmitting = true;

    const request$ = this.editingId
      ? this.apiClient.usersPUT(this.editingId, new UserDto(this.userForm.value))
      : this.apiClient.usersPOST(new CreateUserDto(this.userForm.value));

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadUsers();
        this.notificationService.success(this.editingId ? 'User updated successfully.' : 'User created successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the user. Please try again.');
        this.notificationService.error(this.errorMessage);
      }
    });
  }


  onTableAction(event: DataTableAction<UserSummaryDto>): void {
    if (event.action === 'edit') {
      this.openEditModal(event.row);
    }
  }
}
