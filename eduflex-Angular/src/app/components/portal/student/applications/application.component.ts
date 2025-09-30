import { Client, ApplicationDetailModel } from './../../../../services/api.services';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { DataTablesModule } from 'angular-datatables';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-student-portal',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DataTablesModule, HttpClientModule],
  templateUrl: './application.component.html',
  styleUrls: ['./application.component.css']
})
export class ApplicationComponent implements OnInit, OnDestroy {
  dtOptions: any = {};
  dtTrigger: Subject<any> = new Subject<any>();

  studentName: string = 'Student';
  isLoading = true;
  applications: ApplicationDetailModel[] = [];
  filteredApplications: ApplicationDetailModel[] = [];

  searchTerm: string = '';
  statusFilter: string = 'all';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private appService: Client
  ) {}

  ngOnInit(): void {
    this.initializeDataTable();
    this.loadStudentApplications();
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
      // filtered = filtered.filter(app =>
      //   app.description.toLowerCase().includes(searchLower) ||
      //   app.status.toLowerCase().includes(searchLower) ||
      //   app.applicationType.toLowerCase().includes(searchLower)
      // );
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

 getStatusBadgeClass(status: string | undefined): string {
  if (!status) return 'badge bg-secondary'; 
  switch (status.toLowerCase()) {
    case 'approved': return 'badge bg-success';
    case 'under review': return 'badge bg-warning text-dark';
    case 'rejected': return 'badge bg-danger';
    case 'documents required': return 'badge bg-info';
    default: return 'badge bg-secondary';
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
