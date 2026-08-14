import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Client, InvoiceRecordDto } from '@services/api.services';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { Button } from 'primeng/button';

// The generic data-table hardcodes badge styling for a column literally named
// "status" (enrolment statuses like "Approved"/"Rejected") — renaming it here so
// this table's own `render` function decides the styling instead of falling into
// that unrelated branch. Omits InvoiceRecordDto's own init/toJSON instance
// methods, which a plain object literal (built with `...inv` below) can't
// satisfy — only the data fields are needed for table rendering.
type InvoiceLedgerRow = Omit<InvoiceRecordDto, 'init' | 'toJSON'> & {
  invoiceStatus?: string;
};

@Component({
  selector: 'app-invoice-ledger',
  standalone: true,
  imports: [CommonModule, FormsModule, DataTableComponent, Button],
  templateUrl: './invoice-ledger.component.html',
  styleUrls: ['./invoice-ledger.component.css']
})
export class InvoiceLedgerComponent implements OnInit {
  invoices: InvoiceLedgerRow[] = [];
  isLoading = false;
  categoryFilter = '';
  statusFilter = '';
  pager = new TablePagerState();

  columns: DataTableColumn<InvoiceLedgerRow>[] = [
    { field: 'invoiceNo', title: 'Invoice No.' },
    {
      field: 'recipientName',
      title: 'Recipient',
      render: (value, row) =>
        `${value}<div class="text-muted small">${row.recipientEmail ?? ''}</div>`,
    },
    {
      field: 'category',
      title: 'Category',
      render: (value) => {
        const cls = value === 'Partner' ? 'badge-pill-accent-soft' : 'badge-pill-navy-soft';
        return `<span class="badge-pill ${cls}">${value}</span>`;
      },
    },
    {
      field: 'total',
      title: 'Total',
      className: 'text-end',
      formatter: (value) => Number(value ?? 0).toFixed(2),
    },
    {
      field: 'invoiceStatus',
      title: 'Status',
      className: 'text-center',
      render: (value) => {
        const cls =
          value === 'Paid' ? 'badge-pill-success-soft' :
          value === 'Sent' ? 'badge-pill-navy-soft' :
          value === 'Failed' ? 'badge-pill-error-soft' :
          'badge-pill-muted-soft';
        return `<span class="badge-pill ${cls}">${value}</span>`;
      },
    },
    {
      field: 'sentAt',
      title: 'Sent',
      formatter: (value) => (value ? new Date(value).toLocaleDateString('en-AU') : ''),
    },
    { field: 'actions', title: 'PDF', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<InvoiceLedgerRow>[] = [
    {
      action: 'download',
      label: '',
      icon: 'fa-file-pdf',
      cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => !!row.pdfUrl,
    },
  ];

  constructor(
    private client: Client,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.client
      .invoices(this.categoryFilter || undefined, this.statusFilter || undefined, this.pager.pageNumber, this.pager.pageSize)
      .subscribe({
        next: (result) => {
          this.invoices = (result.items ?? []).map((inv) => ({ ...inv, invoiceStatus: inv.status }));
          this.pager.totalCount = result.totalCount ?? 0;
          this.isLoading = false;
        },
        error: () => { this.isLoading = false; }
      });
  }

  onFilterChange(): void {
    this.pager.goToPage(1);
    this.load();
  }

  onRefresh(): void {
    this.categoryFilter = '';
    this.statusFilter = '';
    this.pager.goToPage(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.load();
  }

  onTableAction(event: DataTableAction<InvoiceLedgerRow>): void {
    if (event.action === 'download') {
      this.download(event.row);
    }
  }

  download(invoice: InvoiceLedgerRow): void {
    this.client.downloadLink(invoice.id!).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/invoice-templates']);
  }
}
