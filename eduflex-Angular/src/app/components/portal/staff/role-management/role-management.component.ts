import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Client,
  RoleDto,
  CreateRoleDto,
  PermissionDto,
  RoleFilterDto,
  RoleTypeEnums,
} from '@services/api.services';
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
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { Button } from 'primeng/button';

interface ModuleGroup {
  moduleName: string;
  permissions: PermissionDto[];
}

const ROLE_TYPE_BADGE_CLASS: Record<string, string> = {
  Admin: 'badge-pill-error-soft',
  Manager: 'badge-pill-accent-soft',
  Staff: 'badge-pill-success-soft',
  Student: 'badge-pill-muted-soft',
  Customer: 'badge-pill-muted-soft',
};

const ROLE_TYPE_DESCRIPTIONS: Record<string, string> = {
  Admin: 'Full administrative access',
  Manager: 'Manages finance and course promotions',
  Staff: 'Front-line staff with limited access',
  Student: 'Standard authenticated student',
  Customer: 'General customer (reserved for future visa module)',
};

@Component({
  selector: 'app-role-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DataTableComponent,
    ModalComponent,
    NotificationComponent,
    Button,
  ],
  templateUrl: './role-management.component.html',
  styleUrls: ['./role-management.component.css'],
})
export class RoleManagementComponent implements OnInit {
  roles: RoleDto[] = [];
  groupedPermissions: ModuleGroup[] = [];
  permissionActionColumns: string[] = [];
  selectedPermissionIds: string[] = [];
  roleTypes: RoleTypeEnums[] = Object.values(RoleTypeEnums);
  roleTypeDescriptions = ROLE_TYPE_DESCRIPTIONS;

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';

  editingId: string | undefined = undefined;
  editingUserCount = 0;

  pager = new TablePagerState();

  permissions!: ModulePermissions;

  roleForm: FormGroup;

  columns: DataTableColumn<RoleDto>[] = [
    { field: 'name', title: 'Name' },
    { field: 'description', title: 'Description' },
    {
      field: 'roleType',
      title: 'Role Type',
      render: (value) =>
        `<span class="badge-pill ${this.roleTypeBadgeClass(value)}">${value ?? '—'}</span>`,
    },
    {
      field: 'permissionIds',
      title: 'Permissions',
      formatter: (value) => (value?.length ?? 0) + ' permission(s)',
    },
    { field: 'userCount', title: 'Users', formatter: (value) => (value ?? 0) + ' user(s)' },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<RoleDto>[] = [];

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
  ) {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      description: ['', [Validators.maxLength(200)]],
      roleType: ['', [Validators.required]],
    });

    this.permissions = this.authHelper.hasRolesPermission();
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
    this.loadPermissions();
  }

  loadRoles(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view roles.');
      return;
    }

    this.isLoading = true;
    const filter = new RoleFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
    });
    this.apiClient.searchRoles(filter).subscribe({
      next: (result) => {
        this.roles = result.items ?? [];
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
    this.loadRoles();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadRoles();
  }

  onRefresh(): void {
    this.pager.search('');
    this.loadRoles();
  }

  private readonly preferredActionOrder = ['View', 'Add', 'Edit', 'Delete'];

  loadPermissions(): void {
    this.apiClient.permissions().subscribe({
      next: (permissions) => {
        const groups = new Map<string, PermissionDto[]>();
        for (const permission of permissions) {
          const moduleName = permission.moduleName || 'Other';
          if (!groups.has(moduleName)) {
            groups.set(moduleName, []);
          }
          groups.get(moduleName)!.push(permission);
        }
        this.groupedPermissions = Array.from(groups.entries()).map(([moduleName, permissions]) => ({
          moduleName,
          permissions,
        }));

        const actions = new Set(permissions.map((p) => p.action).filter((a): a is string => !!a));
        const preferred = this.preferredActionOrder.filter((a) => actions.has(a));
        const extra = Array.from(actions)
          .filter((a) => !this.preferredActionOrder.includes(a))
          .sort();
        this.permissionActionColumns = [...preferred, ...extra];
      },
      error: () => {},
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.roleForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  isPermissionSelected(permissionId: string | undefined): boolean {
    return !!permissionId && this.selectedPermissionIds.includes(permissionId);
  }

  togglePermission(permissionId: string | undefined, checked: boolean): void {
    if (!permissionId) return;
    if (checked) {
      if (!this.selectedPermissionIds.includes(permissionId)) {
        this.selectedPermissionIds.push(permissionId);
      }
    } else {
      this.selectedPermissionIds = this.selectedPermissionIds.filter((id) => id !== permissionId);
    }
  }

  isModuleFullySelected(group: ModuleGroup): boolean {
    return (
      group.permissions.length > 0 &&
      group.permissions.every((p) => this.isPermissionSelected(p.id))
    );
  }

  toggleModule(group: ModuleGroup, checked: boolean): void {
    for (const permission of group.permissions) {
      this.togglePermission(permission.id, checked);
    }
  }

  getPermission(group: ModuleGroup, action: string): PermissionDto | undefined {
    return group.permissions.find((p) => p.action === action);
  }

  isColumnFullySelected(action: string): boolean {
    const permissions = this.groupedPermissions
      .map((g) => this.getPermission(g, action))
      .filter((p): p is PermissionDto => !!p);
    return permissions.length > 0 && permissions.every((p) => this.isPermissionSelected(p.id));
  }

  toggleColumn(action: string, checked: boolean): void {
    for (const group of this.groupedPermissions) {
      const permission = this.getPermission(group, action);
      if (permission) {
        this.togglePermission(permission.id, checked);
      }
    }
  }

  roleTypeBadgeClass(type: unknown): string {
    return (typeof type === 'string' && ROLE_TYPE_BADGE_CLASS[type]) || 'badge-pill-muted-soft';
  }

  openModal(): void {
    this.roleForm.reset();
    this.selectedPermissionIds = [];
    this.editingUserCount = 0;
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  openEditModal(role: RoleDto): void {
    this.editingId = role.id ?? undefined;
    this.editingUserCount = role.userCount ?? 0;
    this.selectedPermissionIds = role.permissionIds ?? [];
    this.roleForm.reset({
      name: role.name,
      description: role.description,
      roleType: role.roleType,
      permissionIds: role.permissionIds,
    });
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.editingId = undefined;
    this.editingUserCount = 0;
    this.selectedPermissionIds = [];
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

    this.roleForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.roleForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const payload = new CreateRoleDto({
      id: this.editingId ? this.editingId : undefined,
      name: this.roleForm.value.name,
      description: this.roleForm.value.description,
      roleType: this.roleForm.value.roleType,
      permissionIds: this.selectedPermissionIds,
    });

    const request$ = !this.editingId
      ? this.apiClient.rolesPOST(payload)
      : this.apiClient.rolesPUT(this.editingId!, payload);

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadRoles();
        this.notificationService.success('Role saved successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(
          err,
          'Something went wrong saving the role. Please try again.',
        );
        this.notificationService.error(this.errorMessage);
      },
    });
  }

  onTableAction(event: DataTableAction<RoleDto>): void {
    if (event.action === 'edit') {
      this.openEditModal(event.row);
    }
  }
}
