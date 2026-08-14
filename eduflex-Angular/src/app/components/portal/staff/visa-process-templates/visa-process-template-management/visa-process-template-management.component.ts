import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { VisaProcessTemplateService } from '@services/visa-process-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { VisaProcessTemplate, templateStatusBadgeClass } from '@app/models/visa-process-template';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

@Component({
  selector: 'app-visa-process-template-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './visa-process-template-management.component.html',
  styleUrls: ['./visa-process-template-management.component.css'],
})
export class VisaProcessTemplateManagementComponent implements OnInit {
  templates: VisaProcessTemplate[] = [];
  isLoading = false;
  permissions!: ModulePermissions;

  columns: DataTableColumn<VisaProcessTemplate>[] = [
    { field: 'name', title: 'Template name' },
    { field: 'country', title: 'Country', className: 'text-center' },
    { field: 'category', title: 'Category', className: 'text-center' },
    {
      field: 'isDefaultForCountry',
      title: 'Default',
      className: 'text-center',
      formatter: (value) => (value ? 'Yes' : ''),
      hideOnLaptop: true,
    },
    {
      field: 'steps',
      title: 'Steps',
      className: 'text-center',
      formatter: (_value, row: VisaProcessTemplate) => row.steps.length,
      hideOnLaptop: true,
    },
    {
      field: 'statusBadge',
      title: 'Status',
      className: 'text-center',
      render: (_value, row: VisaProcessTemplate) =>
        `<span class="badge-pill ${this.statusBadgeClass(row)}">${row.status}</span>`,
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<VisaProcessTemplate>[] = [
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
    private templateService: VisaProcessTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasVisaProcessTemplatesPermission();
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

  statusBadgeClass(template: VisaProcessTemplate): string {
    return templateStatusBadgeClass(template.status);
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/visa-process-templates/new']);
  }

  onTableAction(event: DataTableAction<VisaProcessTemplate>): void {
    switch (event.action) {
      case 'edit':
        this.router.navigate(['/staff-portal/visa-process-templates', event.row.id]);
        break;
      case 'deactivate':
        this.setStatus(event.row, false);
        break;
      case 'activate':
        this.setStatus(event.row, true);
        break;
    }
  }

  private setStatus(template: VisaProcessTemplate, isActive: boolean): void {
    this.templateService.setStatus(template.id, isActive).subscribe({
      next: () => {
        this.notificationService.success(isActive ? 'Template reactivated.' : 'Template deactivated.');
        this.loadTemplates();
      },
      error: (err) => {
        this.notificationService.error(
          extractHttpErrorMessage(err, "Could not update this template's status."),
        );
      },
    });
  }
}
