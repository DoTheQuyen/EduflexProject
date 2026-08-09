import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FinancialRecordService } from '@services/financial-record.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { FinancialRecord } from '../../../../../models/financial-record';

@Component({
  selector: 'app-financial-record-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './financial-record-management.component.html',
  styleUrls: ['./financial-record-management.component.css'],
})
export class FinancialRecordManagementComponent implements OnInit {
  records: FinancialRecord[] = [];

  isLoading = false;
  pager = new TablePagerState();

  permissions!: ModulePermissions;

  columns: DataTableColumn<FinancialRecord>[] = [
    { field: 'studentName', title: 'Student' },
    { field: 'enrolmentStatus', title: 'Enrolment Status', className: 'text-center' },
    {
      field: 'expectedCommission',
      title: 'Expected Commission',
      className: 'text-end',
      formatter: (value) => Number(value ?? 0).toFixed(2),
    },
    {
      field: 'invoices',
      title: 'Invoices',
      className: 'text-center',
      render: (_value, row) => String(row.invoices?.length ?? 0),
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<FinancialRecord>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
  ];

  constructor(
    private financialRecordService: FinancialRecordService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasFinancePermission();
  }

  ngOnInit(): void {
    this.loadRecords();
  }

  loadRecords(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view financial records.');
      return;
    }

    this.isLoading = true;
    this.financialRecordService
      .search({
        pageNumber: this.pager.pageNumber,
        pageSize: this.pager.pageSize,
        searchTerm: this.pager.searchTerm || undefined,
      })
      .subscribe({
        next: (result) => {
          this.records = result.items ?? [];
          this.pager.totalCount = result.totalCount ?? 0;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        },
      });
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadRecords();
  }

  onTableAction(event: DataTableAction<FinancialRecord>): void {
    if (event.action === 'view') {
      this.router.navigate(['/staff-portal/financial-records', event.row.id]);
    }
  }
}
