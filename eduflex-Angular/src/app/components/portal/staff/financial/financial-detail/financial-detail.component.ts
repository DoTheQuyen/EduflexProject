import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FinancialRecordService } from '@services/financial-record.service';
import { EnrolmentService } from '@services/enrolment.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FinancialRecord } from '../../../../../models/financial-record';
import { Enrolment } from '../../../../../models/enrolment';
import { SummaryTabComponent } from './tabs/summary-tab/summary-tab.component';
import { FinanceTabComponent, ClaimInvoiceRequest } from './tabs/finance-tab/finance-tab.component';
import { InvoiceTabComponent } from './tabs/invoice-tab/invoice-tab.component';
import { FinancialCommunicationTabComponent } from './tabs/communication-tab/communication-tab.component';
import { FinancialAuditTabComponent } from './tabs/audit-tab/audit-tab.component';
import { FinancialTasksTabComponent } from './tabs/tasks-tab/tasks-tab.component';

type FinancialTab = 'summary' | 'finance' | 'invoice' | 'communication' | 'audit' | 'tasks';

@Component({
  selector: 'app-financial-detail',
  standalone: true,
  imports: [CommonModule, SummaryTabComponent, FinanceTabComponent, InvoiceTabComponent, FinancialCommunicationTabComponent, FinancialAuditTabComponent, FinancialTasksTabComponent],
  templateUrl: './financial-detail.component.html',
  styleUrls: ['./financial-detail.component.css']
})
export class FinancialDetailComponent implements OnInit {
  financialRecordId = '';
  record: FinancialRecord | null = null;
  enrolment: Enrolment | null = null;
  isLoading = true;
  activeTab: FinancialTab = 'summary';
  pendingClaim: ClaimInvoiceRequest | null = null;

  permissions!: ModulePermissions;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private financialRecordService: FinancialRecordService,
    private enrolmentService: EnrolmentService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasFinancePermission();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.financialRecordId = id;
    this.loadRecord();
  }

  // Same non-blocking-reload pattern as EnrolmentDetailComponent — [hidden] tabs stay
  // mounted, so a child's (changed) event must refresh data without tearing down
  // in-progress edits in the other tabs (e.g. a half-written invoice draft).
  loadRecord(): void {
    const isInitialLoad = this.record === null;
    if (isInitialLoad) { this.isLoading = true; }

    this.financialRecordService.get(this.financialRecordId).subscribe({
      next: (record) => {
        this.record = record;
        this.enrolmentService.get(record.enrolmentId).subscribe({
          next: (enrolment) => {
            this.enrolment = enrolment;
            if (isInitialLoad) { this.isLoading = false; }
          },
          error: () => {
            if (isInitialLoad) { this.isLoading = false; }
          }
        });
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this financial record.');
        this.goBack();
      }
    });
  }

  switchTab(tab: FinancialTab): void {
    this.activeTab = tab;
  }

  onSendClaimInvoice(claim: ClaimInvoiceRequest): void {
    this.pendingClaim = claim;
    this.switchTab('invoice');
  }

  goBack(): void {
    if (this.enrolment) {
      this.router.navigate(['/staff-portal/enrolments', this.enrolment.id]);
    } else {
      this.router.navigate(['/staff-portal/enrolments']);
    }
  }
}
