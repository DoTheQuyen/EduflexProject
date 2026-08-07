import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Client, InvoiceTemplateDto, SetInvoiceTemplateStatusDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ModalComponent } from '@generic/modal/modal.component';
import { buildInvoiceTemplatePreviewHtml } from '@app/shared/utils/invoice-preview.util';

@Component({
  selector: 'app-invoice-template-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent, ModalComponent],
  templateUrl: './invoice-template-management.component.html',
  styleUrls: ['./invoice-template-management.component.css']
})
export class InvoiceTemplateManagementComponent implements OnInit {
  templates: InvoiceTemplateDto[] = [];
  isLoading = false;
  permissions!: ModulePermissions;

  columns: DataTableColumn<InvoiceTemplateDto>[] = [
    { field: 'name', title: 'Template name' },
    { field: 'categoryBadge', title: 'Category', className: 'text-center',
      render: (_value, row: InvoiceTemplateDto) => {
        if (row.category === 'Customer') { return '<span class="badge-pill badge-pill-navy-soft">Customer</span>'; }
        if (row.category === 'Partner') { return '<span class="badge-pill badge-pill-accent-soft">Partner</span>'; }
        return '<span class="badge-pill badge-pill-muted-soft">Custom</span>';
      } },
    { field: 'nextInvoiceNoPreview', title: 'Next invoice no.' },
    { field: 'statusBadge', title: 'Status', className: 'text-center',
      render: (_value, row: InvoiceTemplateDto) => row.isActive
        ? '<span class="badge-pill badge-pill-success-soft">Active</span>'
        : '<span class="badge-pill badge-pill-muted-soft">Inactive</span>' },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

    showPreview = false;
  previewHtml: SafeHtml = '';

  confirmDialog: { message: string; confirmLabel: string; onConfirm: () => void } | null = null;

   rowActions: DataTableRowAction<InvoiceTemplateDto>[] = [
    { action: 'view', label: 'Preview', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-info' },
    { action: 'edit', label: 'Edit', icon: 'fa-pen', cssClass: 'btn btn-sm btn-outline-primary' },
    { action: 'copy', label: 'Copy', icon: 'fa-copy', cssClass: 'btn btn-sm btn-outline-secondary' },
    { action: 'deactivate', label: 'Deactivate', icon: 'fa-power-off', cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => !!row.isActive },
    { action: 'activate', label: 'Reactivate', icon: 'fa-rotate-left', cssClass: 'btn btn-sm btn-outline-success',
      isVisible: (row) => !row.isActive }
  ];

  constructor(
    private client: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {
    this.permissions = this.authHelper.hasInvoiceTemplatesPermission();
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.isLoading = true;
    this.client.invoiceTemplatesAll().subscribe({
      next: (templates) => {
        this.templates = templates;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/invoice-templates/new']);
  }

  openLedger(): void {
    this.router.navigate(['/staff-portal/invoice-templates/ledger']);
  }

  sendCustomInvoice(): void {
    this.router.navigate(['/staff-portal/invoice-templates/send-custom']);
  }

  onTableAction(event: DataTableAction<InvoiceTemplateDto>): void {
    switch (event.action) {
       case 'view':
        this.openPreview(event.row);
        break;
       case 'edit':
        this.router.navigate(['/staff-portal/invoice-templates', event.row.id]);
        break;
      case 'copy':
        this.copyTemplate(event.row);
        break;
      case 'deactivate':
        this.setStatus(event.row, false);
        break;
      case 'activate':
        this.setStatus(event.row, true);
        break;
    }
  }

  copyTemplate(template: InvoiceTemplateDto): void {
    this.requestConfirm(
      `Copy "${template.name}" into a new template? You'll be taken to the new template form to review and save it.`,
      'Copy',
      () => {
        this.notificationService.success(`"${template.name}" copied — review the details and save to create the new template.`);
        this.router.navigate(['/staff-portal/invoice-templates/new'], { state: { copyFrom: template } });
      }
    );
  }

  private requestConfirm(message: string, confirmLabel: string, onConfirm: () => void): void {
    this.confirmDialog = { message, confirmLabel, onConfirm };
  }

  proceedConfirm(): void {
    this.confirmDialog?.onConfirm();
    this.confirmDialog = null;
  }

  cancelConfirm(): void {
    this.confirmDialog = null;
  }

  openPreview(template: InvoiceTemplateDto): void {
    this.previewHtml = this.sanitizer.bypassSecurityTrustHtml(buildInvoiceTemplatePreviewHtml({
      category: template.category ?? 'Customer',
      logoUrl: template.logoUrl,
      senderName: template.senderName ?? '',
      senderAddressLines: template.senderAddressLines ?? [],
      senderAbn: template.senderAbn,
      senderEmail: template.senderEmail ?? '',
      senderPhone: template.senderPhone,
      bankName: template.bankName,
      bankBsb: template.bankBsb,
      bankAccountNumber: template.bankAccountNumber,
      bankAccountName: template.bankAccountName,
      defaultDescription: template.defaultDescription,
      defaultAmount: template.defaultAmount,
      defaultGstRatePercent: template.defaultGstRatePercent,
      invoiceNoPreview: template.nextInvoiceNoPreview ?? ''
    }));
    this.showPreview = true;
  }

  closePreview(): void {
    this.showPreview = false;
  }

      private setStatus(template: InvoiceTemplateDto, isActive: boolean): void {
    const message = isActive
      ? `Reactivate the template "${template.name}"? It will become available again when sending invoices.`
      : `Deactivate the template "${template.name}"? Staff will no longer be able to send invoices from it.`;

    this.requestConfirm(message, isActive ? 'Reactivate' : 'Deactivate', () => {
      this.client.statusPOST3(template.id!, new SetInvoiceTemplateStatusDto({ isActive })).subscribe({
        next: () => {
          this.notificationService.success(isActive ? 'Template reactivated.' : 'Template deactivated.');
          this.loadTemplates();
        },
        error: (err) => {
          this.notificationService.error(extractHttpErrorMessage(err, 'Could not update this template\'s status.'));
        }
      });
    });
  }
}
