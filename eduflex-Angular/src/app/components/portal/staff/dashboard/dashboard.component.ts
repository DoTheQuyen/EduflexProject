import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthHelperService } from '../../../../services/auth-helper.service';
import { RealtimeNotificationService, RealtimeNotificationMessage } from '../../../../services/realtime-notification.service';
import { TrendChartComponent } from './trend-chart/trend-chart.component';
import { StatusBreakdownComponent } from './status-breakdown/status-breakdown.component';

interface ModuleTile {
  module: string;
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TrendChartComponent, StatusBreakdownComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  userInfo: any;
  today = new Date();

  // One tile per module — same keys the backend's DashboardService returns counts for
  // (GET /api/Notifications/summary), and the same ones the sidebar count bubbles key
  // off (see SidebarComponent.itemCount). No Feedback tile — feedback has no open/closed
  // concept, so there's nothing "actionable" to count there.
  readonly modules: ModuleTile[] = [
    { module: 'Enquiry', label: 'Open Enquiries', icon: 'envelope', route: '/staff-portal/enquiries' },
    { module: 'Application', label: 'Pending Applications', icon: 'clipboard-list', route: '/staff-portal/applications' },
    { module: 'Enrolment', label: 'Active Enrolments', icon: 'user-graduate', route: '/staff-portal/enrolments' },
    { module: 'Finance', label: 'Finance Action Queue', icon: 'dollar-sign', route: '/staff-portal/finance/accounts' },
  ];

  // "At a Glance" tiles: real open/actionable record counts, computed server-side by
  // DashboardService and delivered on the same call as the notification list below, so
  // both refresh together on the same poll cycle.
  moduleCounts$: Observable<Record<string, number>>;

  // Recent Notifications panel: the separate, live personal notification feed — not the
  // same thing as the counts above, kept conceptually distinct on purpose.
  notifications$: Observable<RealtimeNotificationMessage[]>;

  constructor(
    private authHelper: AuthHelperService,
    private notificationService: RealtimeNotificationService,
  ) {
    // Assigned here, not as field initializers: this project targets ES2022, where native
    // class-field semantics run field initializers before the constructor body — so
    // `this.notificationService` isn't set yet if assigned at declaration.
    this.moduleCounts$ = this.notificationService.moduleCounts$;
    this.notifications$ = this.notificationService.notifications$;
  }

  ngOnInit(): void {
    this.userInfo = this.authHelper.getCurrentUser();
  }

  get greeting(): string {
    const hour = this.today.getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  moduleRoute(module: string): string {
    return this.modules.find((m) => m.module === module)?.route ?? '/staff-portal/dashboard';
  }

  totalOpenCount(counts: Record<string, number> | null): number {
    return counts ? Object.values(counts).reduce((sum, n) => sum + n, 0) : 0;
  }

  // Bar widths in the "Open Items by Module" chart are relative to whichever module has
  // the most open items — Math.max(1, ...) avoids a divide-by-zero when everything's 0.
  maxModuleCount(counts: Record<string, number> | null): number {
    if (!counts) return 1;
    return Math.max(1, ...this.modules.map((m) => counts[m.module] ?? 0));
  }

  // "Open Items by Module" is a magnitude-comparison bar list, not a nav menu — sort
  // descending so the busiest module surfaces first. (The "At a Glance" tile grid stays
  // in fixed order on purpose: it's positional navigation users memorize the layout of.)
  sortedModules(counts: Record<string, number> | null): ModuleTile[] {
    if (!counts) return this.modules;
    return [...this.modules].sort((a, b) => (counts[b.module] ?? 0) - (counts[a.module] ?? 0));
  }

  dismissNotification(id: string, event: MouseEvent): void {
    event.stopPropagation();
    this.notificationService.clear(id);
  }
}
