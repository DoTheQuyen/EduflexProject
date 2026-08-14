import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AccountsService } from '@services/accounts.service';
import { AccountTimeline, AccountTimelineEntry, AccountType } from '../../../../../models/accounts';

interface YearGroup {
  year: number;
  entries: AccountTimelineEntry[];
  total: number;
  isCurrent: boolean;
}

@Component({
  selector: 'app-account-timeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './account-timeline.component.html',
  styleUrls: ['./account-timeline.component.css']
})
export class AccountTimelineComponent implements OnInit {
  timeline: AccountTimeline | null = null;
  yearGroups: YearGroup[] = [];
  isLoading = false;
  notFound = false;

  private accountType: AccountType = 'Student';
  private accountKey = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private accountsService: AccountsService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.accountType = (params.get('accountType') as AccountType) ?? 'Student';
      this.accountKey = params.get('accountKey') ?? '';
      this.load();
    });
  }

  load(): void {
    if (!this.accountKey) { this.notFound = true; return; }
    this.isLoading = true;
    this.notFound = false;
    this.accountsService.getTimeline(this.accountType, this.accountKey).subscribe({
      next: (timeline) => {
        this.timeline = timeline;
        this.yearGroups = this.groupByYear(timeline.entries);
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; this.notFound = true; }
    });
  }

  private groupByYear(entries: AccountTimelineEntry[]): YearGroup[] {
    const currentYear = new Date().getFullYear();
    const byYear = new Map<number, AccountTimelineEntry[]>();

    for (const entry of entries) {
      const year = new Date(entry.dueDate).getFullYear();
      if (!byYear.has(year)) byYear.set(year, []);
      byYear.get(year)!.push(entry);
    }

    return Array.from(byYear.entries())
      .sort(([a], [b]) => a - b)
      .map(([year, yearEntries]) => ({
        year,
        entries: yearEntries.sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()),
        total: yearEntries.reduce((sum, e) => sum + e.amount, 0),
        isCurrent: year <= currentYear
      }));
  }

  typeLabel(type: string): string {
    switch (type) {
      case 'Student': return 'Student';
      case 'BusinessPartner': return 'Business Partner';
      case 'EducationPartner': return 'Education Partner';
      default: return type;
    }
  }

  feeTypeLabel(feeType: string): string {
    switch (feeType) {
      case 'Tuition': return 'Tuition';
      case 'ServiceFee': return 'Enrolment service fee';
      case 'VisaExtension': return 'Visa extension';
      case 'Visa485': return 'Visa 485';
      case 'PartnerVisa': return 'Partner visa';
      case 'Commission': return 'Commission';
      default: return feeType || 'Other';
    }
  }

  scheduleBadgeClass(status: string): string {
    switch (status) {
      case 'Invoiced': return 'badge-pill-navy-soft';
      case 'Skipped': return 'badge-pill-muted-soft';
      default: return 'badge-pill-accent-soft'; // Planned
    }
  }

  invoiceBadgeClass(status?: string): string {
    switch (status) {
      case 'Paid': return 'badge-pill-success-soft';
      case 'Sent': return 'badge-pill-navy-soft';
      case 'Failed': return 'badge-pill-error-soft';
      case 'Cancelled': return 'badge-pill-muted-soft';
      default: return 'badge-pill-muted-soft';
    }
  }

  receivedPercent(): number {
    if (!this.timeline || this.timeline.contractTotal <= 0) return 0;
    return Math.min(100, Math.round((this.timeline.received / this.timeline.contractTotal) * 100));
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/finance/accounts']);
  }
}
