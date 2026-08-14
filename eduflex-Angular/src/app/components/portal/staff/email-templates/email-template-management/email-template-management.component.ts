import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { EmailTemplateService } from '@services/email-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { EmailTemplate, emailTemplateStatusBadgeClass } from '@app/models/enrolment';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

@Component({
  selector: 'app-email-template-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './email-template-management.component.html',
  styleUrls: ['./email-template-management.component.css'],
})
export class EmailTemplateManagementComponent implements OnInit {
  templates: EmailTemplate[] = [];
  isLoading = false;
  permissions!: ModulePermissions;

  columns: DataTableColumn<EmailTemplate>[] = [
    { field: 'name', title: 'Template name' },
    { field: 'key', title: 'Key' },
    { field: 'subject', title: 'Subject' },
    {
      field: 'systemBadge',
      title: 'Type',
      className: 'text-center',
      render: (_value, row: EmailTemplate) =>
        row.isSystemDefault
          ? '<span class="badge-pill badge-pill-navy-soft">System</span>'
          : '<span class="badge-pill badge-pill-muted-soft">Custom</span>',
      hideOnLaptop: true,
    },
    {
      field: 'statusBadge',
      title: 'Status',
      className: 'text-center',
      render: (_value, row: EmailTemplate) =>
        `<span class="badge-pill ${this.statusBadgeClass(row)}">${row.isActive ? 'Active' : 'Inactive'}</span>`,
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<EmailTemplate>[] = [
    { action: 'edit', label: 'Edit', icon: 'fa-pen', cssClass: 'btn btn-sm btn-outline-primary' },
    {
      action: 'deactivate',
      label: 'Deactivate',
      icon: 'fa-power-off',
      cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => row.isActive,
    },
    {
      action: 'activate',
      label: 'Reactivate',
      icon: 'fa-rotate-left',
      cssClass: 'btn btn-sm btn-outline-success',
      isVisible: (row) => !row.isActive,
    },
  ];

  constructor(
    private templateService: EmailTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasEmailTemplatesPermission();
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

  statusBadgeClass(template: EmailTemplate): string {
    return emailTemplateStatusBadgeClass(template);
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/email-templates/new']);
  }

  onTableAction(event: DataTableAction<EmailTemplate>): void {
    switch (event.action) {
      case 'edit':
        this.router.navigate(['/staff-portal/email-templates', event.row.id]);
        break;
      case 'deactivate':
        this.setStatus(event.row, false);
        break;
      case 'activate':
        this.setStatus(event.row, true);
        break;
    }
  }

  private setStatus(template: EmailTemplate, isActive: boolean): void {
    this.templateService.setStatus(template.id, isActive).subscribe({
      next: () => {
        this.notificationService.success(
          isActive ? 'Template reactivated.' : 'Template deactivated.',
        );
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
