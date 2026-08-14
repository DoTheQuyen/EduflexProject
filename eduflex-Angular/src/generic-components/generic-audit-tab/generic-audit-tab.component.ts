import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { formatDateTime } from '@app/shared/utils/date-time.util';

// Shape any module's audit-entry model can be mapped to — id is optional since some
// callers don't need one (nothing here keys off it, it's just available for trackBy if a
// consumer wants it later).
export interface GenericAuditEntry {
  id?: string;
  description: string;
  performedByName: string;
  performedAt: string;
}

// Fully generic activity-log display — no knowledge of Enrolment, MigrationCase, or any
// other entity. A caller passes its own audit entries (already sorted newest-first, same
// as every backend AuditTrail list in this codebase) and gets the same timeline UI
// Enrolment's audit tab already used. Reused by app-audit-tab (Enrolment) and directly by
// Migration Case's detail page.
@Component({
  selector: 'app-generic-audit-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './generic-audit-tab.component.html',
  styleUrls: ['./generic-audit-tab.component.css']
})
export class GenericAuditTabComponent {
  @Input({ required: true }) entries: GenericAuditEntry[] = [];

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }
}
