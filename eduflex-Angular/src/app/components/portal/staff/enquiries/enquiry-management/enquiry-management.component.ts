import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  Client,
  EnquiryDto,
  EnquiryFilterDto,
  EnquiryEnums,
  EnquiryStatusDto,
} from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { formatDateTime } from '../../../../../shared/utils/date-time.util';

type EnquiryTab = 'New' | 'MIR' | 'Responded' | 'Converted';

@Component({
  selector: 'app-enquiry-management',
  standalone: true,
  imports: [CommonModule, DataTableComponent, NotificationComponent],
  templateUrl: './enquiry-management.component.html',
  styleUrls: ['./enquiry-management.component.css'],
})
export class EnquiryManagementComponent implements OnInit {
  enquiries: EnquiryDto[] = [];
  statusOptions: EnquiryStatusDto[] = [];

  isLoading = false;
  pager = new TablePagerState();
  activeTab: EnquiryTab = 'New';

  permissions!: ModulePermissions;

  newColumns: DataTableColumn<EnquiryDto>[] = [
    {
      field: 'firstName',
      title: 'Name',
      render: (_value, row) => `${row.firstName ?? ''} ${row.lastName ?? ''}`.trim(),
    },
    { field: 'email', title: 'Email', hideOnLaptop: true },
    { field: 'mobile', title: 'Mobile', hideOnLaptop: true },
    { field: 'subject', title: 'Subject' },
    {
      field: 'createdAt',
      title: 'Received',
      className: 'text-center',
      formatter: (value) => formatDateTime(value, 'dd/MM/yyyy HH:mm'),
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  respondedColumns: DataTableColumn<EnquiryDto>[] = [
    {
      field: 'firstName',
      title: 'Name',
      render: (_value, row) => `${row.firstName ?? ''} ${row.lastName ?? ''}`.trim(),
    },
    { field: 'subject', title: 'Subject' },
    { field: 'updatedBy', title: 'Responded By' },
    {
      field: 'updatedAt',
      title: 'Responded At',
      className: 'text-center',
      formatter: (value) => (value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : ''),
    },
    { field: 'response', title: 'Response', formatter: (value) => this.truncateResponse(value) },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  constructor(
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.permissions = this.authHelper.hasEnquiryPermission();

    const savedTab = this.route.snapshot.queryParamMap.get('tab') as EnquiryTab | null;
    if (
      savedTab === 'New' ||
      savedTab === 'MIR' ||
      savedTab === 'Responded' ||
      savedTab === 'Converted'
    ) {
      this.activeTab = savedTab;
    }
  }

  get currentColumns(): DataTableColumn<EnquiryDto>[] {
    return this.activeTab === 'New' ? this.newColumns : this.respondedColumns;
  }

  get rowActions(): DataTableRowAction<EnquiryDto>[] {
    return [
      { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
      ...(this.activeTab === 'New' && this.permissions.delete
        ? [
            {
              action: 'delete',
              label: 'Delete',
              icon: 'fa-trash',
              cssClass: 'btn btn-sm btn-outline-danger',
            },
          ]
        : []),
    ];
  }

  ngOnInit(): void {
    this.loadStatuses();
    this.loadEnquiries();
  }

  loadStatuses(): void {
    this.apiClient.enquiryStatuses().subscribe({
      next: (statuses) => {
        this.statusOptions = statuses;
      },
      error: () => {},
    });
  }

  switchTab(tab: EnquiryTab): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.pager.goToPage(1);
    this.loadEnquiries();
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }

  private statusesForTab(tab: EnquiryTab): EnquiryEnums[] {
    switch (tab) {
      case 'New':
        return [EnquiryEnums.New];
      case 'MIR':
        return [EnquiryEnums.MIR];
      case 'Responded':
        return [EnquiryEnums.Responded];
      case 'Converted':
        return [EnquiryEnums.Converted];
    }
  }

  truncateResponse(value: string | undefined): string {
    if (!value) return '';
    return value.length > 100 ? value.substring(0, 100) + '...' : value;
  }

  loadEnquiries(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view enquiries.');
      return;
    }

    this.isLoading = true;
    const filter = new EnquiryFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      statuses: this.statusesForTab(this.activeTab),
    });
    this.apiClient.searchEnquiries(filter).subscribe({
      next: (result) => {
        this.enquiries = result.items ?? [];
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
    this.loadEnquiries();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadEnquiries();
  }

  onRefresh(): void {
    this.pager.search('');
    this.loadEnquiries();
  }

  onDelete(enquiry: EnquiryDto): void {
    if (!this.permissions.delete) {
      this.notificationService.error('You do not have permission to delete enquiries.');
      return;
    }

    const confirmed = window.confirm(
      `Delete the enquiry from ${enquiry.firstName} ${enquiry.lastName}?`,
    );
    if (!confirmed || !enquiry.id) {
      return;
    }

    this.apiClient.enquiriesDELETE(enquiry.id).subscribe({
      next: () => {
        this.loadEnquiries();
        this.notificationService.success('Enquiry deleted successfully.');
      },
      error: () => {
        this.notificationService.error('Could not delete this enquiry. Please try again.');
      },
    });
  }

  onTableAction(event: DataTableAction<EnquiryDto>): void {
    if (event.action === 'view') {
      this.router.navigate(['/staff-portal/enquiries', event.row.id], {
        queryParams: { tab: this.activeTab },
      });
    } else if (event.action === 'delete') {
      this.onDelete(event.row);
    }
  }
}
