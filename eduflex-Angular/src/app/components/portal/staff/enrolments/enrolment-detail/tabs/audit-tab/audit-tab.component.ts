import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GenericAuditTabComponent } from '@generic/generic-audit-tab/generic-audit-tab.component';
import { Enrolment } from '../../../../../../../models/enrolment';

// Thin Enrolment-specific wrapper around the fully generic app-generic-audit-tab — kept as
// its own component (rather than every call site switching straight to the generic one)
// purely so enrolment-detail.component.html's existing `[enrolment]="enrolment"` binding
// doesn't need to change. See Migration Case's detail page for a call site that uses
// app-generic-audit-tab directly instead.
@Component({
  selector: 'app-audit-tab',
  standalone: true,
  imports: [CommonModule, GenericAuditTabComponent],
  templateUrl: './audit-tab.component.html',
  styleUrls: ['./audit-tab.component.css']
})
export class AuditTabComponent {
  @Input({ required: true }) enrolment!: Enrolment;
}
