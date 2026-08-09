import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Client, BusinessPartnerDto, BusinessPartnerFilterDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';

@Component({
  selector: 'app-business-partner-management',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  templateUrl: './business-partner-management.component.html',
  styleUrls: ['./business-partner-management.component.css'],
})
export class BusinessPartnerManagementComponent implements OnInit {
  partners: BusinessPartnerDto[] = [];

  isLoading = false;
  pager = new TablePagerState();

  permissions!: ModulePermissions;

  columns: DataTableColumn<BusinessPartnerDto>[] = [
    { field: 'name', title: 'Name' },
    { field: 'email', title: 'Email' },
    { field: 'phoneNumber', title: 'Phone' },
    {
      field: 'commissionBaseRate',
      title: 'Commission Rate',
      className: 'text-end',
      render: (value) => `${Number(value ?? 0)}%`,
    },
    {
      field: 'contractEndDate',
      title: 'Contract Status',
      className: 'text-center',
      render: (_value, row) => this.contractStatusBadge(row),
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<BusinessPartnerDto>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
  ];

  constructor(
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
  ) {
    this.permissions = this.authHelper.hasBusinessPartnersPermission();
  }

  ngOnInit(): void {
    this.loadPartners();
  }

  private contractStatusBadge(row: BusinessPartnerDto): string {
    if (!row.contractEndDate) {
      return '<span class="badge-pill badge-pill-navy-soft">Ongoing</span>';
    }
    const isExpired = new Date(row.contractEndDate as unknown as string) < new Date();
    return isExpired
      ? '<span class="badge-pill badge-pill-error-soft">Expired</span>'
      : '<span class="badge-pill badge-pill-success-soft">Active</span>';
  }

  loadPartners(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view business partners.');
      return;
    }

    this.isLoading = true;
    const filter = new BusinessPartnerFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
    });

    this.apiClient.searchBusinessPartners(filter).subscribe({
      next: (result) => {
        this.partners = result.items ?? [];
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
    this.loadPartners();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadPartners();
  }

  onTableAction(event: DataTableAction<BusinessPartnerDto>): void {
    if (event.action === 'view') {
      this.router.navigate(['/staff-portal/business-partners', event.row.id, 'edit']);
    }
  }

  createNew(): void {
    this.router.navigate(['/staff-portal/business-partners/new']);
  }
}
