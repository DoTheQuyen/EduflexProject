import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountsService } from '@services/accounts.service';
import { AccountSummary, AccountStatus, AccountType, ActionQueueItem, ActionQueueResult } from '../../../../../models/accounts';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { Button } from 'primeng/button';

export type AccountsTab = 'queue' | 'all';

// The generic data-table hardcodes badge styling for a column literally named
// "status" — renamed here so this table's own `render` decides the styling instead of
// falling into that unrelated branch (see the same fix in invoice-ledger.component.ts).
type AccountRow = AccountSummary & { acctStatus?: string };

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DataTableComponent, Button],
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.css']
})
export class AccountsComponent implements OnInit {
  // Queue and portfolio are the same underlying dataset viewed two ways, so they live
  // on one page as tabs (queue is the default) instead of two separate nav entries.
  activeTab: AccountsTab = 'queue';

  queueResult: ActionQueueResult | null = null;
  queueLoading = false;
  queueLoaded = false;
  windowDays = 14;

  accounts: AccountRow[] = [];
  isLoading = false;
  accountsLoaded = false;
  search = '';
  accountType: AccountType | '' = '';
  status: AccountStatus | '' = '';
  pager = new TablePagerState();

  columns: DataTableColumn<AccountRow>[] = [
    {
      field: 'name',
      title: 'Account',
      render: (value, row) => row.subLabel
        ? `${value}<div class="text-muted small">${row.subLabel}</div>`
        : `${value}`,
    },
    {
      field: 'accountType',
      title: 'Type',
      render: (value) => `<span class="badge-pill badge-pill-navy-soft">${this.typeLabel(value as string)}</span>`,
    },
    {
      field: 'contractTotal',
      title: 'Contract',
      className: 'text-end',
      formatter: (value) => Number(value ?? 0).toFixed(2),
    },
    {
      field: 'received',
      title: 'Received',
      render: (value, row) => {
        const pct = row.contractTotal > 0 ? Math.min(100, Math.round((row.received / row.contractTotal) * 100)) : 0;
        return `<div class="acct-progress"><div class="acct-progress-track"><div class="acct-progress-fill" style="width:${pct}%"></div></div><span>${pct}%</span></div>`;
      },
    },
    {
      field: 'nextDueDate',
      title: 'Next due',
      formatter: (value) => (value ? new Date(value as string).toLocaleDateString('en-AU') : '—'),
    },
    {
      field: 'acctStatus',
      title: 'Status',
      className: 'text-center',
      render: (value) => `<span class="badge-pill ${this.statusBadgeClass(value as string)}">${this.statusLabel(value as string)}</span>`,
    },
    { field: 'openCount', title: 'Open', className: 'text-center' },
    { field: 'actions', title: '', className: 'text-end' },
  ];

  rowActions: DataTableRowAction<AccountRow>[] = [
    { action: 'open', label: 'Open', cssClass: 'btn btn-sm btn-outline-primary' },
  ];

  constructor(private accountsService: AccountsService, private router: Router) {}

  ngOnInit(): void {
    this.loadQueue();
  }

  switchTab(tab: AccountsTab): void {
    this.activeTab = tab;
    if (tab === 'queue' && !this.queueLoaded) this.loadQueue();
    if (tab === 'all' && !this.accountsLoaded) this.load();
  }

  loadQueue(): void {
    this.queueLoading = true;
    this.accountsService.getActionQueue(this.windowDays).subscribe({
      next: (result) => { this.queueResult = result; this.queueLoading = false; this.queueLoaded = true; },
      error: () => { this.queueLoading = false; }
    });
  }

  reasonLabel(item: ActionQueueItem): string {
    switch (item.reason) {
      case 'Overdue': return `Overdue ${item.days} day${item.days === 1 ? '' : 's'}`;
      case 'Failed': return 'Send failed — needs resend';
      case 'NotInvoiced': return item.days === 0 ? 'Due today, not yet invoiced' : `Not yet invoiced — due in ${item.days} day${item.days === 1 ? '' : 's'}`;
      default: return item.reason;
    }
  }

  reasonBadgeClass(item: ActionQueueItem): string {
    switch (item.reason) {
      case 'Overdue': return 'badge-pill-error-soft';
      case 'Failed': return 'badge-pill-error-soft';
      default: return 'badge-pill-accent-soft';
    }
  }

  openQueueItem(item: ActionQueueItem): void {
    this.router.navigate(['/staff-portal/finance/accounts/timeline'], {
      queryParams: { accountType: item.accountType, accountKey: item.accountKey }
    });
  }

  load(): void {
    this.isLoading = true;
    this.accountsService
      .getAccounts({
        search: this.search || undefined,
        accountType: (this.accountType || undefined) as AccountType | undefined,
        status: (this.status || undefined) as AccountStatus | undefined,
        pageNumber: this.pager.pageNumber,
        pageSize: this.pager.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.accounts = (result.items ?? []).map((a) => ({ ...a, acctStatus: a.status }));
          this.pager.totalCount = result.totalCount ?? 0;
          this.isLoading = false;
          this.accountsLoaded = true;
        },
        error: () => { this.isLoading = false; },
      });
  }

  onFilterChange(): void {
    this.pager.goToPage(1);
    this.load();
  }

  onRefresh(): void {
    if (this.activeTab === 'queue') {
      this.loadQueue();
      return;
    }
    this.search = '';
    this.accountType = '';
    this.status = '';
    this.pager.goToPage(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.load();
  }

  onTableAction(event: DataTableAction<AccountRow>): void {
    if (event.action === 'open') {
      this.router.navigate(['/staff-portal/finance/accounts/timeline'], {
        queryParams: { accountType: event.row.accountType, accountKey: event.row.accountKey },
      });
    }
  }

  typeLabel(type: string): string {
    switch (type) {
      case 'Student': return 'Student';
      case 'BusinessPartner': return 'Business Partner';
      case 'EducationPartner': return 'Education Partner';
      default: return type;
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'OnTrack': return 'On track';
      case 'AtRisk': return 'At risk';
      case 'Overdue': return 'Overdue';
      case 'Complete': return 'Complete';
      default: return status;
    }
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'OnTrack': return 'badge-pill-navy-soft';
      case 'AtRisk': return 'badge-pill-warning-soft';
      case 'Overdue': return 'badge-pill-error-soft';
      case 'Complete': return 'badge-pill-success-soft';
      default: return 'badge-pill-muted-soft';
    }
  }
}
