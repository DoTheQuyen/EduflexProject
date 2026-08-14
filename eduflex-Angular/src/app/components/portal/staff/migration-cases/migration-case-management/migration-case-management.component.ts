import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MigrationCaseService } from '@services/migration-case.service';
import { VisaProcessTemplateService } from '@services/visa-process-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction } from '@generic/data-table/data-table.models';
import { MigrationCase, caseStatusBadgeClass } from '@app/models/migration-case';
import { VisaProcessTemplate } from '@app/models/visa-process-template';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

@Component({
  selector: 'app-migration-case-management',
  standalone: true,
  imports: [CommonModule, FormsModule, DataTableComponent],
  templateUrl: './migration-case-management.component.html',
  styleUrls: ['./migration-case-management.component.css'],
})
export class MigrationCaseManagementComponent implements OnInit {
  cases: MigrationCase[] = [];
  isLoading = false;
  permissions!: ModulePermissions;

  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  searchTerm = '';

  showStartPanel = false;
  activeTemplates: VisaProcessTemplate[] = [];
  isCreating = false;
  newCaseTemplateId = '';
  newCaseContactName = '';
  newCaseContactEmail = '';
  newCaseContactMobile = '';
  newCaseNotes = '';

  columns: DataTableColumn<MigrationCase>[] = [
    { field: 'caseReference', title: 'Case #' },
    { field: 'primaryContactName', title: 'Contact' },
    { field: 'category', title: 'Category', className: 'text-center' },
    { field: 'country', title: 'Country', className: 'text-center', hideOnLaptop: true },
    { field: 'templateName', title: 'Template', hideOnLaptop: true },
    {
      field: 'statusBadge',
      title: 'Status',
      className: 'text-center',
      render: (_value, row: MigrationCase) =>
        `<span class="badge-pill ${caseStatusBadgeClass(row.status)}">${row.status}</span>`,
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  constructor(
    private caseService: MigrationCaseService,
    private templateService: VisaProcessTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasMigrationCasesPermission();
  }

  ngOnInit(): void {
    this.loadCases();
  }

  loadCases(): void {
    this.isLoading = true;
    this.caseService.search({
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || undefined,
    }).subscribe({
      next: (result) => {
        this.cases = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  onPageChange(page: number): void {
    this.pageNumber = page;
    this.loadCases();
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
    this.loadCases();
  }

  onRefresh(): void {
    this.searchTerm = '';
    this.pageNumber = 1;
    this.loadCases();
  }

  onTableAction(event: DataTableAction<MigrationCase>): void {
    if (event.action === 'view') {
      this.router.navigate(['/staff-portal/migration-cases', event.row.id]);
    }
  }

  openStartPanel(): void {
    this.showStartPanel = true;
    if (this.activeTemplates.length === 0) {
      this.templateService.getAll().subscribe({
        next: (templates) => {
          this.activeTemplates = templates.filter((t) => t.status === 'Active');
          if (this.activeTemplates.length > 0) {
            this.newCaseTemplateId = this.activeTemplates[0].id;
          }
        },
      });
    }
  }

  closeStartPanel(): void {
    this.showStartPanel = false;
    this.newCaseTemplateId = '';
    this.newCaseContactName = '';
    this.newCaseContactEmail = '';
    this.newCaseContactMobile = '';
    this.newCaseNotes = '';
  }

  startCase(): void {
    if (!this.newCaseTemplateId || !this.newCaseContactName.trim()) {
      this.notificationService.error('Choose a template and enter the primary contact name.');
      return;
    }

    this.isCreating = true;
    this.caseService.create({
      templateId: this.newCaseTemplateId,
      primaryContactName: this.newCaseContactName.trim(),
      primaryContactEmail: this.newCaseContactEmail.trim() || undefined,
      primaryContactMobile: this.newCaseContactMobile.trim() || undefined,
      notes: this.newCaseNotes.trim() || undefined,
    }).subscribe({
      next: (created) => {
        this.isCreating = false;
        this.notificationService.success(`Case ${created.caseReference} started.`);
        this.router.navigate(['/staff-portal/migration-cases', created.id]);
      },
      error: (err) => {
        this.isCreating = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not start this case.'));
      },
    });
  }
}
