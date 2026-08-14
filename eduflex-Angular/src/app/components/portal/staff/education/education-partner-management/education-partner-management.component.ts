import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Client, EducationPartnerDto, EducationPartnerFilterDto, CourseDto, CourseSearchFilterDto, CourseSearchResultDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { Button } from 'primeng/button';

interface CountryGroup {
  country: string;
  universities: EducationPartnerDto[];
  colleges: EducationPartnerDto[];
}

// A common shape for a shortlisted course regardless of where it was added from —
// the Course Search results (CourseSearchResultDto) and a partner's own course list
// (CourseDto, which has no university/country of its own) carry the same underlying
// data under different shapes.
interface ShortlistItem {
  courseId: string;
  educationPartnerId: string;
  courseName: string;
  uniName: string;
  country?: string;
  campuses: string[];
  intakes: string[];
  studyModes: string[];
  courseDurationMonths?: number;
  tuitionFee?: number;
  totalTuitionFee?: number;
  tuitionCurrency?: string;
}

const PARTNER_TYPE_COLLEGE = 'College/Education Organization';

const COURSE_LEVEL_PREFIXES: { prefix: string; level: string }[] = [
  { prefix: 'graduate diploma', level: 'Graduate Diploma' },
  { prefix: 'graduate certificate', level: 'Graduate Certificate' },
  { prefix: 'doctor of philosophy', level: 'PhD' },
  { prefix: 'phd', level: 'PhD' },
  { prefix: 'master', level: 'Master' },
  { prefix: 'bachelor', level: 'Bachelor' },
  { prefix: 'diploma', level: 'Diploma' },
  { prefix: 'certificate', level: 'Certificate' }
];

@Component({
  selector: 'app-education-partner-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DataTableComponent, Button],
  templateUrl: './education-partner-management.component.html',
  styleUrls: ['./education-partner-management.component.css']
})
export class EducationPartnerManagementComponent implements OnInit {
  activeTab: 'uni' | 'search' | 'shortlist' = 'uni';

  isLoadingDirectory = false;
  countryGroups: CountryGroup[] = [];
  expandedCountries = new Set<string>();
  expandedPartners = new Set<string>();
  private courseFilters = new Map<string, string>();

  isSearching = false;
  hasSearched = false;
  searchResults: CourseSearchResultDto[] = [];
  searchPager = new TablePagerState();
  searchFilters = { courseName: '', uniName: '', country: '', intake: '', campus: '', studyMode: '', maxTuition: null as number | null };

  // Compare tool for a live consultation — hold a few candidates side by side on
  // their own tab instead of scrolling back and forth in a results table. Capped at
  // 4 so the compare table stays scannable; not persisted, scoped to this session.
  shortlistMax = 4;
  shortlist: ShortlistItem[] = [];

  // The shortlist action sits first so it's visible without scrolling right once a
  // staff member has already scrolled down through the results on a laptop screen.
  searchColumns: DataTableColumn<CourseSearchResultDto>[] = [
    { field: 'actions', title: '', sortable: false, minWidth: '100px' },
    { field: 'courseName', title: 'Course', render: (value, row) => `${value}<span class="course-level-tag">${this.courseLevel(row.courseName)}</span>` },
    { field: 'uniName', title: 'University' },
    { field: 'campuses', title: 'Campus', render: (_value, row) => (row.campuses ?? []).join(', ') || '—' },
    { field: 'country', title: 'Country', hideOnLaptop: true },
    { field: 'intakes', title: 'Intakes', render: (_value, row) => (row.intakes ?? []).join(', ') },
    { field: 'studyModes', title: 'Mode / Duration',
      render: (_value, row) => {
        const mode = (row.studyModes ?? []).join(', ') || '—';
        const duration = row.courseDurationMonths ? `${row.courseDurationMonths} mo` : '';
        return [mode, duration].filter(Boolean).join(' · ');
      } },
    { field: 'tuitionFee', title: 'Tuition (p.a.)', className: 'text-end',
      render: (value, row) => `${row.tuitionCurrency} ${Number(value).toLocaleString()}` }
  ];

  searchRowActions: DataTableRowAction<CourseSearchResultDto>[] = [
    { action: 'shortlist', label: 'Shortlist', icon: 'fa-plus', cssClass: 'btn btn-sm btn-outline-secondary',
      isVisible: (row) => !this.isShortlisted(row.id) && this.shortlist.length < this.shortlistMax },
    { action: 'unshortlist', label: 'Shortlisted', icon: 'fa-check', cssClass: 'btn btn-sm btn-primary',
      isVisible: (row) => this.isShortlisted(row.id) }
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

  switchTab(tab: 'uni' | 'search' | 'shortlist'): void {
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

  // A partner has no campus of its own — each course lists the campus(es) it runs
  // at, so the partner's campus list is the union of its courses' campuses.
  getPartnerCampuses(partner: EducationPartnerDto): string[] {
    const campuses = new Set<string>();
    for (const course of partner.courses ?? []) {
      for (const campus of course.campuses ?? []) {
        campuses.add(campus);
      }
    }
    return Array.from(campuses).sort();
  }

  courseLevel(courseName: string | undefined): string {
    const lower = (courseName ?? '').trim().toLowerCase();
    const match = COURSE_LEVEL_PREFIXES.find((entry) => lower.startsWith(entry.prefix));
    return match?.level ?? 'Course';
  }

  getLevelBreakdown(partner: EducationPartnerDto): { level: string; count: number }[] {
    const counts = new Map<string, number>();
    for (const course of partner.courses ?? []) {
      const level = this.courseLevel(course.courseName);
      counts.set(level, (counts.get(level) ?? 0) + 1);
    }
    return Array.from(counts.entries()).map(([level, count]) => ({ level, count }));
  }

  getCourseFilter(partnerId: string | undefined): string {
    return (partnerId && this.courseFilters.get(partnerId)) || '';
  }

  setCourseFilter(partnerId: string | undefined, value: string): void {
    if (!partnerId) { return; }
    this.courseFilters.set(partnerId, value);
  }

  getFilteredCourses(partner: EducationPartnerDto): CourseDto[] {
    const courses = partner.courses ?? [];
    const query = this.getCourseFilter(partner.id).trim().toLowerCase();
    if (!query) { return courses; }
    return courses.filter((course) =>
      (course.courseName ?? '').toLowerCase().includes(query) ||
      (course.campuses ?? []).some((campus) => campus.toLowerCase().includes(query)) ||
      (course.intakes ?? []).some((intake) => intake.toLowerCase().includes(query))
    );
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

  onRefresh(): void {
    this.searchFilters = { courseName: '', uniName: '', country: '', intake: '', campus: '', studyMode: '', maxTuition: null };
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
      intake: this.searchFilters.intake || undefined,
      campus: this.searchFilters.campus || undefined,
      studyMode: this.searchFilters.studyMode || undefined,
      maxTuition: this.searchFilters.maxTuition ?? undefined
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

  // ----- Shortlist / compare -----

  isShortlisted(courseId: string | undefined): boolean {
    return !!courseId && this.shortlist.some((c) => c.courseId === courseId);
  }

  onSearchTableAction(event: DataTableAction<CourseSearchResultDto>): void {
    if (event.action === 'shortlist' || event.action === 'unshortlist') {
      this.toggleShortlistFromSearch(event.row);
    }
  }

  toggleShortlistFromSearch(row: CourseSearchResultDto): void {
    this.toggleShortlistItem({
      courseId: row.id ?? '',
      educationPartnerId: row.educationPartnerId ?? '',
      courseName: row.courseName ?? '',
      uniName: row.uniName ?? '',
      country: row.country,
      campuses: row.campuses ?? [],
      intakes: row.intakes ?? [],
      studyModes: row.studyModes ?? [],
      courseDurationMonths: row.courseDurationMonths,
      tuitionFee: row.tuitionFee,
      totalTuitionFee: row.totalTuitionFee,
      tuitionCurrency: row.tuitionCurrency
    });
  }

  toggleShortlistFromPartner(partner: EducationPartnerDto, course: CourseDto): void {
    this.toggleShortlistItem({
      courseId: course.id ?? '',
      educationPartnerId: partner.id ?? '',
      courseName: course.courseName ?? '',
      uniName: partner.name ?? '',
      country: partner.country,
      campuses: course.campuses ?? [],
      intakes: course.intakes ?? [],
      studyModes: course.studyModes ?? [],
      courseDurationMonths: course.courseDurationMonths,
      tuitionFee: course.tuitionFee,
      totalTuitionFee: course.totalTuitionFee,
      tuitionCurrency: course.tuitionCurrency
    });
  }

  private toggleShortlistItem(item: ShortlistItem): void {
    if (!item.courseId) { return; }
    const index = this.shortlist.findIndex((c) => c.courseId === item.courseId);
    if (index > -1) {
      this.shortlist.splice(index, 1);
      return;
    }
    if (this.shortlist.length >= this.shortlistMax) { return; }
    this.shortlist.push(item);
  }

  removeFromShortlist(courseId: string): void {
    this.shortlist = this.shortlist.filter((c) => c.courseId !== courseId);
  }

  clearShortlist(): void {
    this.shortlist = [];
  }

  enrolmentQueryParams(item: ShortlistItem): Record<string, string> {
    return { educationPartnerId: item.educationPartnerId, courseId: item.courseId };
  }

  // Comparing raw tuition numbers across different currencies would be misleading,
  // so the lowest-tuition highlight only applies when every shortlisted item quotes
  // the same currency. Duration has no such caveat. Both need 2+ items — with a
  // single course there's nothing to compare, so "lowest" would be meaningless.
  private get minTuition(): number | null {
    if (this.shortlist.length < 2) { return null; }
    const currencies = new Set(this.shortlist.map((c) => c.tuitionCurrency));
    if (currencies.size > 1) { return null; }
    const values = this.shortlist.map((c) => c.tuitionFee).filter((v): v is number => v != null);
    return values.length ? Math.min(...values) : null;
  }

  private get minDuration(): number | null {
    if (this.shortlist.length < 2) { return null; }
    const values = this.shortlist.map((c) => c.courseDurationMonths).filter((v): v is number => v != null);
    return values.length ? Math.min(...values) : null;
  }

  isBestTuition(item: ShortlistItem): boolean {
    return this.minTuition != null && item.tuitionFee === this.minTuition;
  }

  isBestDuration(item: ShortlistItem): boolean {
    return this.minDuration != null && item.courseDurationMonths === this.minDuration;
  }
}
