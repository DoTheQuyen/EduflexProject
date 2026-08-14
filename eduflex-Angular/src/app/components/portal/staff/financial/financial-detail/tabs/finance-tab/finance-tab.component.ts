import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client } from '@services/api.services';
import { FinancialRecordService } from '@services/financial-record.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { FinancialRecord, FinanceInvoice, InvoicePlanEntry } from '../../../../../../../models/financial-record';

// Carries the claim being fulfilled up to FinancialDetailComponent, which switches to
// the Invoice tab and hands it down as InvoiceTabComponent's pendingClaim input.
export interface ClaimInvoiceRequest {
  entryId: string;
  description: string;
  amount: number;
}

@Component({
  selector: 'app-finance-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './finance-tab.component.html',
  styleUrls: ['./finance-tab.component.css']
})
export class FinanceTabComponent implements OnChanges {
  @Input({ required: true }) record!: FinancialRecord;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();
  @Output() sendClaimInvoice = new EventEmitter<ClaimInvoiceRequest>();

  businessPartnerName: string | null = null;
  educationPartnerName: string | null = null;

  showAdjustmentForm = false;
  adjustmentReason = '';
  adjustmentAmount: number | null = null;
  isSavingAdjustment = false;

  paidInvoices: FinanceInvoice[] = [];
  isLoadingInvoices = false;

  editingEntryId: string | null = null;
  editingDate = '';
  skipDialogEntry: InvoicePlanEntry | null = null;
  skipReason = '';
  isMutatingPlan = false;
  showManualForm = false;
  manualDate = '';

  constructor(
    private apiClient: Client,
    private financialRecordService: FinancialRecordService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['record'] || !this.record) return;

    if (this.record.businessPartnerId) {
      this.apiClient.businessPartnersGET(this.record.businessPartnerId).subscribe({
        next: (bp) => { this.businessPartnerName = bp.name ?? null; },
        error: () => {}
      });
    }
    if (this.record.educationPartnerId) {
      this.apiClient.educationPartnersGET(this.record.educationPartnerId).subscribe({
        next: (ep) => { this.educationPartnerName = ep.name ?? null; },
        error: () => {}
      });
    }

    this.loadInvoices();
  }

  private loadInvoices(): void {
    this.isLoadingInvoices = true;
    this.financialRecordService.getInvoicesForRecord(this.record.id).subscribe({
      next: (invoices) => {
        this.paidInvoices = invoices
          .filter(i => i.status === 'Paid')
          .sort((a, b) => new Date(a.paidAt ?? a.sentAt).getTime() - new Date(b.paidAt ?? b.sentAt).getTime());
        this.isLoadingInvoices = false;
      },
      error: () => { this.isLoadingInvoices = false; }
    });
  }

  get invoiceToLabel(): string {
    return this.businessPartnerName ?? this.educationPartnerName ?? '—';
  }

  get totalAdjustments(): number {
    return this.record.extraCommissionAdjustments.reduce((sum, a) => sum + a.amount, 0);
  }

  get totalCommission(): number {
    return this.record.expectedCommission + this.totalAdjustments;
  }

  get totalReceived(): number {
    return this.paidInvoices.reduce((sum, i) => sum + i.total, 0);
  }

  get receivedPercent(): number {
    if (this.totalCommission <= 0) return 0;
    return Math.min(100, Math.round((this.totalReceived / this.totalCommission) * 1000) / 10);
  }

  get outstanding(): number {
    return Math.max(0, this.totalCommission - this.totalReceived);
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy') : '—';
  }

  toggleAdjustmentForm(): void {
    this.showAdjustmentForm = !this.showAdjustmentForm;
    this.adjustmentReason = '';
    this.adjustmentAmount = null;
  }

  addAdjustment(): void {
    if (!this.adjustmentReason.trim() || this.adjustmentAmount === null) {
      this.notificationService.error('Reason and amount are both required.');
      return;
    }
    this.isSavingAdjustment = true;
    this.financialRecordService.addCommissionAdjustment(this.record.id, this.adjustmentReason.trim(), this.adjustmentAmount).subscribe({
      next: () => {
        this.isSavingAdjustment = false;
        this.showAdjustmentForm = false;
        this.notificationService.success('Commission adjustment added.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingAdjustment = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not add this adjustment. Please try again.'));
      }
    });
  }

  // ----- Invoice request calendar -----

  get intakeMonthsLabel(): string {
    const months = Array.from(new Set(
      this.record.invoicePlan
        .filter(e => e.intakeDate)
        .map(e => new Date(e.intakeDate!).toLocaleString('en-AU', { month: 'long' }))
    ));
    return months.length > 0 ? months.join(', ') : '—';
  }

  // Was totalCommission / every claim ever created (including already-Invoiced/Paid
  // and Skipped ones) — so adding a manual claim after some claims were already
  // fulfilled suggested the full commission again instead of what's actually left.
  // Now: what's still outstanding, split across only the claims still open to invoice.
  get suggestedClaimAmount(): number {
    const openCount = this.record.invoicePlan.filter(e => e.status === 'Planned').length || 1;
    return this.outstanding / openCount;
  }

  claimLabel(entry: InvoicePlanEntry): string {
    const index = this.record.invoicePlan.indexOf(entry);
    return `Claim ${index + 1} of ${this.record.invoicePlan.length}`;
  }

  startEditDate(entry: InvoicePlanEntry): void {
    if (!this.permissions.edit) return;
    this.editingEntryId = entry.id;
    this.editingDate = entry.claimDate.substring(0, 10);
  }

  cancelEditDate(): void {
    this.editingEntryId = null;
    this.editingDate = '';
  }

  saveEditDate(entry: InvoicePlanEntry): void {
    if (!this.editingDate) return;
    this.isMutatingPlan = true;
    this.financialRecordService.updatePlanEntryDate(this.record.id, entry.id, this.editingDate).subscribe({
      next: () => {
        this.isMutatingPlan = false;
        this.editingEntryId = null;
        this.notificationService.success('Claim date updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isMutatingPlan = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not update this claim date.'));
      }
    });
  }

  openSkipDialog(entry: InvoicePlanEntry): void {
    if (!this.permissions.edit) return;
    this.skipDialogEntry = entry;
    this.skipReason = '';
  }

  closeSkipDialog(): void {
    this.skipDialogEntry = null;
    this.skipReason = '';
  }

  confirmSkip(): void {
    const entry = this.skipDialogEntry;
    if (!entry) return;
    this.isMutatingPlan = true;
    this.financialRecordService.skipPlanEntry(this.record.id, entry.id, this.skipReason.trim()).subscribe({
      next: () => {
        this.isMutatingPlan = false;
        this.closeSkipDialog();
        this.notificationService.success('Claim skipped.');
        this.changed.emit();
      },
      error: (err) => {
        this.isMutatingPlan = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not skip this claim.'));
      }
    });
  }

  restoreEntry(entry: InvoicePlanEntry): void {
    if (!this.permissions.edit) return;
    this.isMutatingPlan = true;
    this.financialRecordService.restorePlanEntry(this.record.id, entry.id).subscribe({
      next: () => {
        this.isMutatingPlan = false;
        this.notificationService.success('Claim restored.');
        this.changed.emit();
      },
      error: (err) => {
        this.isMutatingPlan = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not restore this claim.'));
      }
    });
  }

  toggleManualForm(): void {
    this.showManualForm = !this.showManualForm;
    this.manualDate = '';
  }

  addManualEntry(): void {
    if (!this.manualDate) {
      this.notificationService.error('Pick a date for the manual claim.');
      return;
    }
    this.isMutatingPlan = true;
    this.financialRecordService.addManualPlanEntry(this.record.id, this.manualDate).subscribe({
      next: () => {
        this.isMutatingPlan = false;
        this.showManualForm = false;
        this.notificationService.success('Manual claim added.');
        this.changed.emit();
      },
      error: (err) => {
        this.isMutatingPlan = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not add this claim.'));
      }
    });
  }

  regeneratePlan(): void {
    if (!this.permissions.edit) return;
    if (!confirm('Regenerate the claim schedule from the course\'s intake calendar? Already-invoiced and skipped claims are kept; other planned dates are recalculated.')) return;

    this.isMutatingPlan = true;
    this.financialRecordService.regenerateInvoicePlan(this.record.id).subscribe({
      next: () => {
        this.isMutatingPlan = false;
        this.notificationService.success('Invoice request calendar regenerated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isMutatingPlan = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not regenerate the calendar.'));
      }
    });
  }

  requestClaimInvoice(entry: InvoicePlanEntry): void {
    if (!this.permissions.edit) return;
    const index = this.record.invoicePlan.indexOf(entry);
    this.sendClaimInvoice.emit({
      entryId: entry.id,
      description: `Commission claim ${index + 1} of ${this.record.invoicePlan.length}`,
      amount: Math.round(this.suggestedClaimAmount * 100) / 100
    });
  }
}
