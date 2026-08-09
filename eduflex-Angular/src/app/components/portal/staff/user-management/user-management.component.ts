import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Client,
  UserSummaryDto,
  CreateUserDto,
  UserDto,
  RoleSummaryDto,
  UserFilterDto,
  RoleTypeEnums,
} from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

type UserRoleTab = 'Student' | 'Customer' | 'Staffs';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DataTableComponent,
    ModalComponent,
    NotificationComponent,
  ],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css'],
})
export class UserManagementComponent implements OnInit {
  users: UserSummaryDto[] = [];
  roles: RoleSummaryDto[] = [];
  visibleRoles: RoleSummaryDto[] = [];

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();
  roleFilter = '';
  activeFilter = 'all';

  activeTab: UserRoleTab = 'Staffs';

  permissions!: ModulePermissions;

  userForm: FormGroup;

  columns: DataTableColumn<UserSummaryDto>[] = [
    { field: 'email', title: 'Email' },
    { field: 'firstName', title: 'First Name' },
    { field: 'lastName', title: 'Last Name' },
    { field: 'roleName', title: 'Role' },
    {
      field: 'departments',
      title: 'Departments',
      render: (_value, row) => this.renderDepartmentBadges(row),
    },
    { field: 'isActive', title: 'Active', formatter: (value) => (value ? 'Yes' : 'No') },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<UserSummaryDto>[] = [];

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      middleName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      mobile: ['', [Validators.required, Validators.maxLength(20)]],
      roleId: ['', [Validators.required]],
      isActive: [true],
    });

    this.permissions = this.authHelper.hasUsersPermission();

    this.rowActions = [
      ...(this.permissions.edit
        ? [
            {
              action: 'edit',
              label: 'Edit',
              icon: 'fa-edit',
              cssClass: 'btn btn-sm btn-outline-primary',
            },
          ]
        : []),
    ];
  }

  ngOnInit(): void {
    this.loadRoles();
  }

  loadUsers(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view users.');
      return;
    }

    this.isLoading = true;
    const isActive = this.activeFilter === 'all' ? undefined : this.activeFilter === 'true';
    const filter = new UserFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      roleId: this.roleFilter || undefined,
      roleIds: this.visibleRoles.map((r) => r.id).filter((id): id is string => !!id),
      isActive,
    });
    this.apiClient.searchUsers(filter).subscribe({
      next: (result) => {
        this.users = result.items ?? [];
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
    this.apiClient.roleLookup().subscribe({
      next: (result) => {
        this.roles = result ?? [];
        this.updateVisibleRoles();
        this.loadUsers();
      },
      error: () => {
        this.roles = [];
        this.updateVisibleRoles();
        this.loadUsers();
      },
    });
  }

  private updateVisibleRoles(): void {
    this.visibleRoles = this.roles.filter((r) =>
      this.roleTypeMatchesTab(r.roleType, this.activeTab),
    );
  }

  private roleTypeMatchesTab(roleType: RoleTypeEnums | undefined, tab: UserRoleTab): boolean {
    if (roleType === undefined) return false;
    if (tab === 'Student') return roleType === RoleTypeEnums.Student;
    if (tab === 'Customer') return roleType === RoleTypeEnums.Customer;
    return roleType !== RoleTypeEnums.Student && roleType !== RoleTypeEnums.Customer;
  }

  switchTab(tab: UserRoleTab): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.roleFilter = '';
    this.updateVisibleRoles();
    this.pager.goToPage(1);
    this.loadUsers();
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }

  // TODO(department-migration): drop the `as any` once `nswag run` has been re-run
  // against the updated backend — UserSummaryDto doesn't declare `departments` yet
  // because api.services.ts hasn't been regenerated, even though the server already
  // sends it (see UsersController.SearchUsers / DepartmentMappingExtension.ToBadges).
  renderDepartmentBadges(row: UserSummaryDto): string {
    const departments = (row as any).departments as
      | { id: string; name: string; isHead: boolean }[]
      | undefined;
    if (!departments || departments.length === 0) {
      return '<span class="text-muted small">&mdash;</span>';
    }

    return departments
      .map((d) => {
        const cssClass = d.isHead ? 'badge-pill-accent' : 'badge-pill-navy-soft';
        const label = this.escapeHtml(d.name) + (d.isHead ? ' (Head)' : '');
        return `<span class="badge-pill ${cssClass} me-1 mb-1">${label}</span>`;
      })
      .join('');
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
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
      middleName: (user as any).middleName,
      lastName: user.lastName,
      mobile: user.mobile,
      roleId: user.roleId,
      isActive: user.isActive,
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
    const requiredPermission = this.editingId ? this.permissions.edit : this.permissions.add;
    if (!requiredPermission) {
      this.notificationService.error(
        this.editingId
          ? 'You do not have permission to edit users.'
          : 'You do not have permission to add users.',
      );
      return;
    }

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
        this.notificationService.success(
          this.editingId ? 'User updated successfully.' : 'User created successfully.',
        );
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(
          err,
          'Something went wrong saving the user. Please try again.',
        );
        this.notificationService.error(this.errorMessage);
      },
    });
  }

  onTableAction(event: DataTableAction<UserSummaryDto>): void {
    if (event.action === 'edit') {
      this.openEditModal(event.row);
    }
  }
}
