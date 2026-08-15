import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthHelperService } from '../../../../services/auth-helper.service';
import { RealtimeNotificationService, RealtimeNotificationMessage } from '../../../../services/realtime-notification.service';

interface DashboardCard {
  title: string;
  description: string;
  icon: string;
  route: string;
}

interface ModuleTile {
  module: string;
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  userInfo: any;
  today = new Date();
  cards: DashboardCard[] = [];

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

  // "At a Glance" tiles and Quick Access badges: real open/actionable record counts,
  // computed server-side by DashboardService and delivered on the same call as the
  // notification list below, so both refresh together on the same poll cycle.
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
    this.setCards();
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

  // Quick Access cards don't fetch their own data — this just looks up a count already
  // pulled in for the "At a Glance" tiles above, so a card like "Applications" can show
  // how many are outstanding without a second API call.
  cardCount(card: DashboardCard, counts: Record<string, number> | null): number {
    const module = this.modules.find((m) => m.route === card.route)?.module;
    return module && counts ? (counts[module] ?? 0) : 0;
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

  private setCards(): void {
    const allCards: DashboardCard[] = [
      { title: 'Applications', description: 'Review student applications', icon: 'clipboard-list', route: '/staff-portal/applications' },
      { title: 'Feedback', description: 'Read student feedback', icon: 'comment', route: '/staff-portal/feedback' },
      { title: 'Course Promotions', description: 'Manage promotional campaigns', icon: 'bullhorn', route: '/staff-portal/course-promotions' },
      { title: 'Roles', description: 'Configure role permissions', icon: 'lock', route: '/staff-portal/roles' },
      { title: 'Users', description: 'Manage staff and student accounts', icon: 'users', route: '/staff-portal/users' }
    ];

    this.cards = this.userInfo?.role === 'Admin'
      ? allCards
      : allCards.filter(c => c.title !== 'Roles' && c.title !== 'Users');
  }
}