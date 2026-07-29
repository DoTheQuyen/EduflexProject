import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client } from '@services/api.services';
import { FinancialRecordService } from '@services/financial-record.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { FinancialRecord } from '../../../../../../../models/financial-record';

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

  businessPartnerName: string | null = null;
  educationPartnerName: string | null = null;

  showAdjustmentForm = false;
  adjustmentReason = '';
  adjustmentAmount: number | null = null;
  isSavingAdjustment = false;

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
}
