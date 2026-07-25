import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, RoleDto, CreateRoleDto, PermissionDto } from '@services/api.services';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn } from '@generic/data-table/data-table.models';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

interface ModuleGroup {
  moduleName: string;
  permissions: PermissionDto[];
}

@Component({
  selector: 'app-role-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTableComponent, ModalComponent, NotificationComponent],
  templateUrl: './role-management.component.html',
  styleUrls: ['./role-management.component.css']
})
export class RoleManagementComponent implements OnInit {
  roles: RoleDto[] = [];
  groupedPermissions: ModuleGroup[] = [];
  selectedPermissionIds: string[] = [];

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';

  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;

  roleForm: FormGroup;

  columns: DataTableColumn<RoleDto>[] = [
    { field: 'name', title: 'Name' },
    { field: 'description', title: 'Description' },
    { field: 'permissionIds', title: 'Permissions', formatter: (value) => (value?.length ?? 0) + ' permission(s)' }
  ];

  constructor(private fb: FormBuilder, private apiClient: Client, private notificationService: NotificationService) {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      description: ['', [Validators.maxLength(200)]]
    });
  }

  ngOnInit(): void {
    this.loadRoles();
    this.loadPermissions();
  }

  loadRoles(): void {
    this.isLoading = true;
    this.apiClient.rolesGET(this.pageNumber, this.pageSize).subscribe({
      next: (result) => {
        this.roles = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.pageNumber = page;
    this.loadRoles();
  }

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
        this.groupedPermissions = Array.from(groups.entries()).map(([moduleName, permissions]) => ({ moduleName, permissions }));
      },
      error: () => {}
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
      this.selectedPermissionIds = this.selectedPermissionIds.filter(id => id !== permissionId);
    }
  }

  openModal(): void {
    this.roleForm.reset();
    this.selectedPermissionIds = [];
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    this.roleForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.roleForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const payload = new CreateRoleDto({
      name: this.roleForm.value.name,
      description: this.roleForm.value.description,
      permissionIds: this.selectedPermissionIds
    });

    this.apiClient.rolesPOST(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadRoles();
        this.notificationService.success('Role saved successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the role. Please try again.');
        this.notificationService.error(this.errorMessage);
      }
    });
  }
}
