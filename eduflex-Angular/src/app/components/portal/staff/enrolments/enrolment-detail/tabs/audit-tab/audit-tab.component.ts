import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment } from '../../../../../../../models/enrolment';

@Component({
  selector: 'app-audit-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit-tab.component.html',
  styleUrls: ['./audit-tab.component.css']
})
export class AuditTabComponent {
  @Input({ required: true }) enrolment!: Enrolment;

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }
}
