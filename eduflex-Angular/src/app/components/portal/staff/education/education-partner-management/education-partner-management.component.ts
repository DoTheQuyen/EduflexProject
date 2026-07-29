import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Client, EducationPartnerDto, EducationPartnerFilterDto, CourseSearchFilterDto, CourseSearchResultDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';

interface CountryGroup {
  country: string;
  universities: EducationPartnerDto[];
  colleges: EducationPartnerDto[];
}

const PARTNER_TYPE_COLLEGE = 'College/Education Organization';

@Component({
  selector: 'app-education-partner-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DataTableComponent],
  templateUrl: './education-partner-management.component.html',
  styleUrls: ['./education-partner-management.component.css']
})
export class EducationPartnerManagementComponent implements OnInit {
  activeTab: 'uni' | 'search' = 'uni';

  isLoadingDirectory = false;
  countryGroups: CountryGroup[] = [];
  expandedCountries = new Set<string>();
  expandedPartners = new Set<string>();

  isSearching = false;
  hasSearched = false;
  searchResults: CourseSearchResultDto[] = [];
  searchPager = new TablePagerState();
  searchFilters = { courseName: '', uniName: '', country: '', intake: '' };

  searchColumns: DataTableColumn<CourseSearchResultDto>[] = [
    { field: 'courseName', title: 'Course' },
    { field: 'uniName', title: 'University' },
    { field: 'country', title: 'Country' },
    { field: 'intakes', title: 'Intakes', render: (_value, row) => (row.intakes ?? []).join(', ') },
    { field: 'tuitionFee', title: 'Tuition', className: 'text-end',
      render: (value, row) => `${row.tuitionCurrency} ${Number(value).toLocaleString()}` }
  ];

  permissions!: ModulePermissions;

  constructor(
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasEducationPartnersPermission();
  }

  ngOnInit(): void {
    this.loadDirectory();
  }

  switchTab(tab: 'uni' | 'search'): void {
    this.activeTab = tab;
    if (tab === 'search' && !this.hasSearched) {
      this.onSearchSubmit();
    }
  }

  loadDirectory(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view education partners.');
      return;
    }

    this.isLoadingDirectory = true;
    const filter = new EducationPartnerFilterDto({ pageNumber: 1, pageSize: 100 });
    this.apiClient.searchEducationPartners(filter).subscribe({
      next: (result) => {
        this.countryGroups = this.groupByCountry(result.items ?? []);
        this.isLoadingDirectory = false;
      },
      error: () => {
        this.isLoadingDirectory = false;
      }
    });
  }

  private groupByCountry(partners: EducationPartnerDto[]): CountryGroup[] {
    const map = new Map<string, CountryGroup>();
    for (const partner of partners) {
      const country = partner.country || 'Unspecified';
      if (!map.has(country)) {
        map.set(country, { country, universities: [], colleges: [] });
      }
      const group = map.get(country)!;
      if (partner.partnerType === PARTNER_TYPE_COLLEGE) {
        group.colleges.push(partner);
      } else {
        group.universities.push(partner);
      }
    }
    return Array.from(map.values()).sort((a, b) => a.country.localeCompare(b.country));
  }

  toggleCountry(country: string): void {
    if (this.expandedCountries.has(country)) {
      this.expandedCountries.delete(country);
    } else {
      this.expandedCountries.add(country);
    }
  }

  isCountryExpanded(country: string): boolean {
    return this.expandedCountries.has(country);
  }

  togglePartner(partnerId: string | undefined): void {
    if (!partnerId) { return; }
    if (this.expandedPartners.has(partnerId)) {
      this.expandedPartners.delete(partnerId);
    } else {
      this.expandedPartners.add(partnerId);
    }
  }

  isPartnerExpanded(partnerId: string | undefined): boolean {
    return !!partnerId && this.expandedPartners.has(partnerId);
  }

  onDeletePartner(partner: EducationPartnerDto): void {
    if (!this.permissions.delete) {
      this.notificationService.error('You do not have permission to delete education partners.');
      return;
    }

    const confirmed = window.confirm(`Delete the education partner "${partner.name}"? This also deletes all of its courses.`);
    if (!confirmed || !partner.id) {
      return;
    }

    this.apiClient.educationPartnersDELETE(partner.id).subscribe({
      next: () => {
        this.loadDirectory();
        this.notificationService.success('Education partner deleted successfully.');
      },
      error: () => {
        this.notificationService.error('Could not delete this education partner. Please try again.');
      }
    });
  }

  onSearchSubmit(): void {
    this.searchPager.goToPage(1);
    this.fetchCourses();
  }

  onSearchPageChange(page: number): void {
    this.searchPager.goToPage(page);
    this.fetchCourses();
  }

  private fetchCourses(): void {
    this.isSearching = true;
    this.hasSearched = true;
    const filter = new CourseSearchFilterDto({
      pageNumber: this.searchPager.pageNumber,
      pageSize: this.searchPager.pageSize,
      courseName: this.searchFilters.courseName || undefined,
      uniName: this.searchFilters.uniName || undefined,
      country: this.searchFilters.country || undefined,
      intake: this.searchFilters.intake || undefined
    });
    this.apiClient.searchCourses(filter).subscribe({
      next: (result) => {
        this.searchResults = result.items ?? [];
        this.searchPager.totalCount = result.totalCount ?? 0;
        this.isSearching = false;
      },
      error: () => {
        this.isSearching = false;
      }
    });
  }
}
