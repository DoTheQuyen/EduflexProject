import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DataTablesModule } from 'angular-datatables';
import { Client, UserSummaryDto, CreateUserDto, RoleDto } from '@services/api.services';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn } from '@generic/data-table/data-table.models';
import { ModalComponent } from '@generic/modal/modal.component';
import { NotificationComponent } from '@generic/notification/notification.component';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTablesModule, DataTableComponent, ModalComponent, NotificationComponent],
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

  userForm: FormGroup;

  columns: DataTableColumn<UserSummaryDto>[] = [
    { field: 'email', title: 'Email' },
    { field: 'firstName', title: 'First Name' },
    { field: 'lastName', title: 'Last Name' },
    { field: 'roleName', title: 'Role' },
    { field: 'isActive', title: 'Active', formatter: (value) => value ? 'Yes' : 'No' }
  ];

  constructor(private fb: FormBuilder, private apiClient: Client) {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      roleId: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.apiClient.usersAll().subscribe({
      next: (users) => {
        this.users = users;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  loadRoles(): void {
    this.apiClient.rolesAll().subscribe({
      next: (roles) => {
        this.roles = roles;
      },
      error: () => {}
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.userForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openModal(): void {
    this.userForm.reset();
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
    const payload = new CreateUserDto(this.userForm.value);

    this.apiClient.users(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadUsers();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error || 'Something went wrong creating the user. Please try again.';
      }
    });
  }
}
