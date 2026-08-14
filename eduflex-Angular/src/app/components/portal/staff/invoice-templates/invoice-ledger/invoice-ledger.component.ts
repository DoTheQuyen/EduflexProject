import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Client, InvoiceRecordDto } from '@services/api.services';
import { NotificationService } from '@services/notification.service';

@Component({
  selector: 'app-invoice-ledger',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invoice-ledger.component.html',
  styleUrls: ['./invoice-ledger.component.css']
})
export class InvoiceLedgerComponent implements OnInit {
  invoices: InvoiceRecordDto[] = [];
  isLoading = false;
  categoryFilter = '';
  statusFilter = '';

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
    this.client.invoices(this.categoryFilter || undefined, this.statusFilter || undefined, undefined, undefined).subscribe({
      next: (result) => {
        this.invoices = result.items ?? [];
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  download(invoice: InvoiceRecordDto): void {
    this.client.downloadLink(invoice.id!).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/invoice-templates']);
  }
}
