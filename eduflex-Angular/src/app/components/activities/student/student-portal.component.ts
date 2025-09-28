import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DataTablesModule } from 'angular-datatables';
// import type { ADTSettings } from 'angular-datatables/src/models/settings'; 

type AppRow = {
  id: number;
  description: string;
  dateApplied: Date;
  status: string;
};

@Component({
  selector: 'app-student-portal',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DataTablesModule],
  templateUrl: './student-portal.component.html',
  styleUrls: ['./student-portal.component.css']
})

export class StudentPortalComponent implements OnInit {
  activeMenu: string = 'applications';
  currentRoute: string = 'My Applications';
  studentName: string = 'Student';
  isLoading = false;
  dtOptions: any = { pagingType: 'simple_numbers', pageLength: 10 };


  
  // Sample applications data
   applications: AppRow[] = [
    { id: 1, description: 'MBA - University of Sydney',    dateApplied: new Date('2024-01-15'), status: 'Under Review' },
    { id: 2, description: 'BSc - University of Melbourne', dateApplied: new Date('2024-01-10'), status: 'Approved' },
    { id: 3, description: 'Diploma - TAFE Queensland',     dateApplied: new Date('2024-01-20'), status: 'Rejected' },
    { id: 4, description: 'MEng - UNSW',                   dateApplied: new Date('2024-01-05'), status: 'Documents Required' }
  ];

  filteredApplications = [...this.applications];
  searchTerm: string = '';
  statusFilter: string = 'all';

  constructor(private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    // Set active menu based on route
    this.route.url.subscribe(url => {
      if (url.length > 0) {
        this.activeMenu = url[0].path;
        this.updateCurrentRoute();
      }
    });
  }

  setActiveMenu(menu: string): void {
    this.activeMenu = menu;
    this.updateCurrentRoute();
    this.router.navigate([`/student-portal/${menu}`]);
  }

  updateCurrentRoute(): void {
    switch (this.activeMenu) {
      case 'profile':
        this.currentRoute = 'My Profile';
        break;
      case 'applications':
        this.currentRoute = 'My Applications';
        break;
      case 'documents':
        this.currentRoute = 'My Documents';
        break;
      case 'messages':
        this.currentRoute = 'Messages';
        break;
      default:
        this.currentRoute = 'Dashboard';
    }
  }

  filterApplications(): void {
    // this.filteredApplications = this.applications.filter(app => {
    //   const matchesSearch = app.courseName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
    //                        app.institution.toLowerCase().includes(this.searchTerm.toLowerCase());
    //   const matchesStatus = this.statusFilter === 'all' || app.status === this.statusFilter;
    //   return matchesSearch && matchesStatus;
    // });
  }

  onSearchChange(): void {
    this.filterApplications();
  }

  onStatusFilterChange(): void {
    this.filterApplications();
  }

  getStatusBadgeClass(statusColor: string): string {
    switch (statusColor) {
      case 'success': return 'status-badge success';
      case 'warning': return 'status-badge warning';
      case 'danger': return 'status-badge danger';
      case 'info': return 'status-badge info';
      default: return 'status-badge';
    }
  }

  viewApplicationDetails(applicationId: number): void {
    // Navigate to application details page
    this.router.navigate([`/student-portal/applications/${applicationId}`]);
  }

  newApplication(): void {
    this.router.navigate(['/student-portal/new-application']);
  }

  getApplicationCountByStatus(status: string): number {
    return this.applications.filter(app => app.status === status).length;
  }

    // called by the "New Application" button
  onNewApplication(): void {
    this.router.navigate(['/student-portal/new-application']);
  }

  // called by "View Details" in the table
  viewDetails(id: number): void {
    this.router.navigate(['/student-portal/applications', id]);
  }
}