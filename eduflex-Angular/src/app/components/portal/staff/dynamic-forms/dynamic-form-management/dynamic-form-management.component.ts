import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { DynamicFormTemplate, templateStatusBadgeClass } from '@app/models/dynamic-form';
import { VISA_STEP_LABELS, VisaStepKey } from '@app/models/enrolment';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

@Component({
  selector: 'app-dynamic-form-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './dynamic-form-management.component.html',
  styleUrls: ['./dynamic-form-management.component.css'],
})
export class DynamicFormManagementComponent implements OnInit {
  templates: DynamicFormTemplate[] = [];
  isLoading = false;
  permissions!: ModulePermissions;

  columns: DataTableColumn<DynamicFormTemplate>[] = [
    { field: 'name', title: 'Form name' },
    {
      field: 'boundStepKey',
      title: 'Bound step',
      className: 'text-center',
      formatter: (value) => this.stepLabel(value),
    },
    {
      field: 'questions',
      title: 'Questions',
      className: 'text-center',
      formatter: (_value, row: DynamicFormTemplate) => row.questions.length,
    },
    // Named 'statusBadge' rather than 'status' to avoid the data-table's built-in
    // status-column special-casing (hardcoded to Application-module status strings) —
    // renders our own badge-pill markup instead, matching this module's Active/Inactive.
    {
      field: 'statusBadge',
      title: 'Status',
      className: 'text-center',
      render: (_value, row: DynamicFormTemplate) =>
        `<span class="badge-pill ${this.statusBadgeClass(row)}">${row.status}</span>`,
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<DynamicFormTemplate>[] = [
    { action: 'edit', label: 'Edit', icon: 'fa-pen', cssClass: 'btn btn-sm btn-outline-primary' },
    {
      action: 'deactivate',
      label: 'Deactivate',
      icon: 'fa-power-off',
      cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => row.status === 'Active',
    },
    {
      action: 'activate',
      label: 'Reactivate',
      icon: 'fa-rotate-left',
      cssClass: 'btn btn-sm btn-outline-success',
      isVisible: (row) => row.status === 'Inactive',
    },
  ];

  constructor(
    private templateService: DynamicFormTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasDynamicFormsPermission();
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.isLoading = true;
    this.templateService.getAll().subscribe({
      next: (templates) => {
        this.templates = templates;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  stepLabel(key?: string): string {
    if (!key) return 'Not bound';
    return VISA_STEP_LABELS[key as VisaStepKey] ?? key;
  }

  statusBadgeClass(template: DynamicFormTemplate): string {
    return templateStatusBadgeClass(template.status);
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/dynamic-forms/new']);
  }

  onTableAction(event: DataTableAction<DynamicFormTemplate>): void {
    switch (event.action) {
      case 'edit':
        this.router.navigate(['/staff-portal/dynamic-forms', event.row.id]);
        break;
      case 'deactivate':
        this.setStatus(event.row, false);
        break;
      case 'activate':
        this.setStatus(event.row, true);
        break;
    }
  }

  private setStatus(template: DynamicFormTemplate, isActive: boolean): void {
    this.templateService.setStatus(template.id, isActive).subscribe({
      next: () => {
        this.notificationService.success(isActive ? 'Form reactivated.' : 'Form deactivated.');
        this.loadTemplates();
      },
      error: (err) => {
        this.notificationService.error(
          extractHttpErrorMessage(err, "Could not update this form's status."),
        );
      },
    });
  }
}
