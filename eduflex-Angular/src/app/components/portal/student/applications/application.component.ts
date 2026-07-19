import { Client, ApplicationDetailDto } from '@services/api.services';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { DataTablesModule } from 'angular-datatables';
import { AuthHelperService } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction  } from '@generic/data-table/data-table.models';
import { formatDateTime } from '../../../../shared/utils/date-time.util';

@Component({
  selector: 'app-student-portal',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DataTablesModule, DataTableComponent],
  templateUrl: './application.component.html',
  // styleUrls: ['./application.component.css']
})
export class ApplicationComponent implements OnInit, OnDestroy {
  userInfo: any;
  dtOptions: any = {};
  dtTrigger: Subject<any> = new Subject<any>();

  studentName: string = 'Student';
  isLoading = true;
  applications: ApplicationDetailDto[] = [];
  filteredApplications: ApplicationDetailDto[] = [];

  searchTerm: string = '';
  statusFilter: string = 'all';

  columns: DataTableColumn<ApplicationDetailDto>[] = [
  { field: 'description', title: 'Description' },
  { field: 'applicationType', title: 'Type' },
  { field: 'dateApplied', title: 'Date Applied', className: 'text-center',
    formatter: (value) => formatDateTime(value, 'dd/MM/yyyy HH:mm') },
  { field: 'status', title: 'Status', className: 'text-center' },
  { field: 'actions', title: 'Actions', className: 'text-center' }
];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private appService: Client,
    private authHelper: AuthHelperService
  ) {}

  ngOnInit(): void {
    this.initializeDataTable();
    this.loadStudentApplications();
    this.userInfo = this.authHelper.getCurrentUser();
  }

  ngOnDestroy(): void {
    this.dtTrigger.unsubscribe();
  }

  initializeDataTable(): void {
    this.dtOptions = {
      paging: true,
      pagingType: 'simple_numbers',
      pageLength: 10,
      lengthMenu: [5, 10, 25, 50],
      searching: true,
      ordering: true,
      order: [[2, 'desc']], // sort by DateApplied
      info: true,
      autoWidth: false,
      responsive: true,
      language: {
        searchPlaceholder: 'Search applications...'
      }
    };
  }

  loadStudentApplications(): void {
    // 🔑 Assume we store studentId in localStorage after login
    const userData = localStorage.getItem('userData');
    const userId: string = userData ? JSON.parse(userData).id ?? '' : '';

    this.appService.applicationsAll().subscribe({
      next: (apps) => {
        this.applications = apps;
        this.filteredApplications = [...apps];
        this.isLoading = false;
        this.dtTrigger.next(null); // trigger datatable render
      },
      error: (err) => {
        console.error('Error loading applications', err);
        this.isLoading = false;
      }
    });
  }

  filterApplications(): void {
    let filtered = this.applications;

    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filtered = filtered.filter(app =>
        app.description?.toLowerCase().includes(searchLower) ||
        app.status?.toLowerCase().includes(searchLower) ||
        app.applicationType?.toLowerCase().includes(searchLower)
      );
    }

    if (this.statusFilter !== 'all') {
      filtered = filtered.filter(app => app.status === this.statusFilter);
    }

    this.filteredApplications = filtered;
    this.dtTrigger.next(null);
  }

  onSearchChange(event: any): void {
    this.searchTerm = event.target.value;
    this.filterApplications();
  }

  onStatusFilterChange(event: any): void {
    this.statusFilter = event.target.value;
    this.filterApplications();
  }

onTableAction(event: DataTableAction<any>) {
    console.log('Table action clicked:', event);
    if (event.action === 'view') {
      // do something with event.row
      alert(`Viewing application with ID: ${event.row.id}`);
    }
  }
  onNewApplication(): void {
    this.router.navigate(['/student-portal/new-application']);
  }

 viewDetails(id: string | undefined): void {
  if (!id) {
    console.warn('Application ID is missing, cannot navigate.');
    return;
  }
  this.router.navigate(['/student-portal/applications', id]);
}
}
