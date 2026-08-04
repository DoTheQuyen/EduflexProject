import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Client, CreateInvoiceTemplateDto, InvoiceTemplateDto, UpdateInvoiceTemplateDto } from '@services/api.services';
import { NotificationService } from '@services/notification.service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { ModalComponent } from '@generic/modal/modal.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { buildInvoiceTemplatePreviewHtml } from '@app/shared/utils/invoice-preview.util';

@Component({
  selector: 'app-invoice-template-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent, ModalComponent],
  templateUrl: './invoice-template-edit.component.html',
  styleUrls: ['./invoice-template-edit.component.css']
})
export class InvoiceTemplateEditComponent implements OnInit {
  isEditMode = false;
  templateId: string | null = null;
  isLoading = false;
  isSaving = false;
  isUploadingLogo = false;

  name = '';
  category: 'Customer' | 'Partner' = 'Customer';
  logoUrl: string | null = null;
  senderName = '';
  senderAddressLines: string[] = [''];
  senderAbn = '';
  senderEmail = '';
  senderPhone = '';
  bankName = '';
  bankBsb = '';
  bankAccountNumber = '';
  bankAccountName = '';
  invoiceNoPrefix = 'INV-Eduflex';
  numberPadding = 4;
  nextInvoiceNoPreview = '';

  
  defaultDescription = '';
  defaultAmount: number | null = null;
  defaultGstRatePercent: number | null = null;
  showPreview = false;
  previewHtml: SafeHtml = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private client: Client,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.applyCopySource();
      return;
    }

    this.isEditMode = true;
    this.templateId = id;
    this.isLoading = true;
    this.client.invoiceTemplatesGET(id).subscribe({
      next: (template) => {
        this.name = template.name ?? '';
        this.category = (template.category as 'Customer' | 'Partner') ?? 'Customer';
        this.logoUrl = template.logoUrl ?? null;
        this.senderName = template.senderName ?? '';
        this.senderAddressLines = template.senderAddressLines?.length ? [...template.senderAddressLines] : [''];
        this.senderAbn = template.senderAbn ?? '';
        this.senderEmail = template.senderEmail ?? '';
        this.senderPhone = template.senderPhone ?? '';
        this.bankName = template.bankName ?? '';
        this.bankBsb = template.bankBsb ?? '';
        this.bankAccountNumber = template.bankAccountNumber ?? '';
        this.bankAccountName = template.bankAccountName ?? '';
        this.invoiceNoPrefix = template.invoiceNoPrefix ?? 'INV-Eduflex';
        this.numberPadding = template.numberPadding ?? 4;
        this.nextInvoiceNoPreview = template.nextInvoiceNoPreview ?? '';
        this.defaultDescription = template.defaultDescription ?? '';
        this.defaultAmount = template.defaultAmount ?? null;
        this.defaultGstRatePercent = template.defaultGstRatePercent ?? null;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this invoice template.');
        this.goBack();
      }
    });
  }

  private applyCopySource(): void {
    const source = history.state?.copyFrom as InvoiceTemplateDto | undefined;
    if (!source) { return; }

    this.name = source.name ? `${source.name} (Copy)` : '';
    this.category = (source.category as 'Customer' | 'Partner') ?? 'Customer';
    this.logoUrl = source.logoUrl ?? null;
    this.senderName = source.senderName ?? '';
    this.senderAddressLines = source.senderAddressLines?.length ? [...source.senderAddressLines] : [''];
    this.senderAbn = source.senderAbn ?? '';
    this.senderEmail = source.senderEmail ?? '';
    this.senderPhone = source.senderPhone ?? '';
    this.bankName = source.bankName ?? '';
    this.bankBsb = source.bankBsb ?? '';
    this.bankAccountNumber = source.bankAccountNumber ?? '';
    this.bankAccountName = source.bankAccountName ?? '';
    this.invoiceNoPrefix = source.invoiceNoPrefix ?? 'INV-Eduflex';
    this.numberPadding = source.numberPadding ?? 4;
    this.defaultDescription = source.defaultDescription ?? '';
    this.defaultAmount = source.defaultAmount ?? null;
    this.defaultGstRatePercent = source.defaultGstRatePercent ?? null;
  }

  // Live preview only matters before creation — once saved, the real preview comes
  // straight from the server (NextInvoiceNoPreview), since NextSequence only advances
  // server-side when an invoice is actually sent.
  get livePreview(): string {
    if (this.isEditMode) { return this.nextInvoiceNoPreview; }
    const padding = Math.max(1, this.numberPadding || 1);
    return `${this.invoiceNoPrefix || 'INV'}-${'1'.padStart(padding, '0')}`;
  }

  // Plain trackBy-by-index — without this, *ngFor over a primitive string array
  // re-keys by value, so every keystroke looks like "old value removed, new value
  // inserted" and Angular tears down and recreates the <input>, dropping focus.
  trackByIndex(index: number): number {
    return index;
  }

  addAddressLine(): void {
    this.senderAddressLines.push('');
  }

  removeAddressLine(index: number): void {
    this.senderAddressLines.splice(index, 1);
    if (this.senderAddressLines.length === 0) { this.senderAddressLines.push(''); }
  }

  onLogoSelected(file: File): void {
    this.isUploadingLogo = true;
    this.client.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.isUploadingLogo = false;
        this.logoUrl = result.url ?? null;
      },
      error: () => {
        this.isUploadingLogo = false;
        this.notificationService.error('Logo upload failed. Please try again.');
      }
    });
  }

  openPreview(): void {
    this.previewHtml = this.sanitizer.bypassSecurityTrustHtml(buildInvoiceTemplatePreviewHtml({
      logoUrl: this.logoUrl,
      senderName: this.senderName,
      senderAddressLines: this.senderAddressLines,
      senderAbn: this.senderAbn,
      senderEmail: this.senderEmail,
      senderPhone: this.senderPhone,
      bankName: this.bankName,
      bankBsb: this.bankBsb,
      bankAccountNumber: this.bankAccountNumber,
      bankAccountName: this.bankAccountName,
      defaultDescription: this.defaultDescription,
      defaultAmount: this.defaultAmount,
      defaultGstRatePercent: this.defaultGstRatePercent,
      invoiceNoPreview: this.livePreview
    }));
    this.showPreview = true;
  }

  closePreview(): void {
    this.showPreview = false;
  }

  get isValid(): boolean {
    return !!this.name.trim() && !!this.senderName.trim() && !!this.senderEmail.trim() &&
      (this.isEditMode || !!this.invoiceNoPrefix.trim());
  }

  save(): void {
    if (!this.isValid) {
      this.notificationService.error('Give the template a name, sender name and sender email.');
      return;
    }

    this.isSaving = true;
    const addressLines = this.senderAddressLines.map(l => l.trim()).filter(Boolean);

    // Kept as two branches — Create returns the full InvoiceTemplateDto, Update returns
    // a bool, same "can't unify the Observable types across a ternary" reasoning as
    // EmailTemplateEditComponent.save().
    if (this.isEditMode && this.templateId) {
      this.client.invoiceTemplatesPUT(this.templateId, new UpdateInvoiceTemplateDto({
        name: this.name.trim(),
        logoUrl: this.logoUrl ?? undefined,
        senderName: this.senderName.trim(),
        senderAddressLines: addressLines,
        senderAbn: this.senderAbn.trim() || undefined,
        senderEmail: this.senderEmail.trim(),
        senderPhone: this.senderPhone.trim() || undefined,
        bankName: this.bankName.trim() || undefined,
        bankBsb: this.bankBsb.trim() || undefined,
        bankAccountNumber: this.bankAccountNumber.trim() || undefined,
        bankAccountName: this.bankAccountName.trim() || undefined,
        defaultDescription: this.defaultDescription.trim() || undefined,
        defaultAmount: this.defaultAmount ?? undefined,
        defaultGstRatePercent: this.defaultGstRatePercent ?? undefined
      })).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err: unknown) => this.onSaveError(err)
      });
    } else {
      this.client.invoiceTemplatesPOST(new CreateInvoiceTemplateDto({
        name: this.name.trim(),
        category: this.category,
        logoUrl: this.logoUrl ?? undefined,
        senderName: this.senderName.trim(),
        senderAddressLines: addressLines,
        senderAbn: this.senderAbn.trim() || undefined,
        senderEmail: this.senderEmail.trim(),
        senderPhone: this.senderPhone.trim() || undefined,
        bankName: this.bankName.trim() || undefined,
        bankBsb: this.bankBsb.trim() || undefined,
        bankAccountNumber: this.bankAccountNumber.trim() || undefined,
        bankAccountName: this.bankAccountName.trim() || undefined,
        invoiceNoPrefix: this.invoiceNoPrefix.trim(),
        numberPadding: this.numberPadding,
        defaultDescription: this.defaultDescription.trim() || undefined,
        defaultAmount: this.defaultAmount ?? undefined,
        defaultGstRatePercent: this.defaultGstRatePercent ?? undefined
      })).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err: unknown) => this.onSaveError(err)
      });
    }
  }

  private onSaveSuccess(): void {
    this.isSaving = false;
    this.notificationService.success(this.isEditMode ? 'Invoice template updated.' : 'Invoice template created.');
    this.goBack();
  }

  private onSaveError(err: unknown): void {
    this.isSaving = false;
    this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this invoice template.'));
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/invoice-templates']);
  }
}
