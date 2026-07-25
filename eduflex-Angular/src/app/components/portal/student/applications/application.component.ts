import { Client, ApplicationDto } from '@services/api.services';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthHelperService } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction  } from '@generic/data-table/data-table.models';
import { formatDateTime } from '../../../../shared/utils/date-time.util';

@Component({
  selector: 'app-student-portal',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DataTableComponent],
  templateUrl: './application.component.html',
  // styleUrls: ['./application.component.css']
})
export class ApplicationComponent implements OnInit {
  userInfo: any;

  studentName: string = 'Student';
  isLoading = true;
  applications: ApplicationDto[] = [];
  filteredApplications: ApplicationDto[] = [];

  searchTerm: string = '';
  statusFilter: string = 'all';

  columns: DataTableColumn<ApplicationDto>[] = [
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
    this.loadStudentApplications();
    this.userInfo = this.authHelper.getCurrentUser();
  }

  loadStudentApplications(): void {
    // A student's own application list is inherently small (bounded by what one
    // person has applied to) and the search/filter below runs client-side across
    // the whole list, so we fetch one generous page (the server's max page size)
    // rather than wiring real prev/next paging into this screen.
    this.appService.applicationsGET(1, 100).subscribe({
      next: (result) => {
        const apps = result.items ?? [];
        this.applications = apps;
        this.filteredApplications = [...apps];
        this.isLoading = false;
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
