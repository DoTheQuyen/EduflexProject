import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { FinancialRecord } from '../../../../../../../models/financial-record';

// Copy of enrolment-detail's audit-tab component, bound to FinancialRecord.auditTrail
// instead of Enrolment.auditTrail — same `Create()`-factory-backed audit convention on
// the backend (FinancialAuditEntryModel).
@Component({
  selector: 'app-financial-audit-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit-tab.component.html',
  styleUrls: ['./audit-tab.component.css']
})
export class FinancialAuditTabComponent {
  @Input({ required: true }) record!: FinancialRecord;

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }
}
