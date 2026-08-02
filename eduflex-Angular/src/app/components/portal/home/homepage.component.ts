import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthHelperService } from '../../../services/auth-helper.service';
import { RealtimeNotificationService } from '../../../services/realtime-notification.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { NotificationBellComponent } from '../share-component/notification-bell/notification-bell.component';

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, SidebarComponent, NotificationBellComponent],
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.css']
})
export class HomepageComponent implements OnInit, OnDestroy {
  userInfo: any;
  isSidebarCollapsed = false;
  showLogoutConfirm = false;
  currentBreadcrumb = '';
  portalRootLabel = 'Staff Portal';
  portalRootLink = '/staff-portal';

  private routerSub?: Subscription;

  constructor(
    private authHelper: AuthHelperService,
    private notificationService: RealtimeNotificationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (!this.authHelper.isLoggedIn()) {
      this.authHelper.logout();
      this.router.navigate(['/'], { replaceUrl: true });
      return;
    }

    this.userInfo = this.authHelper.getCurrentUser();

    if (this.userInfo?.role === 'Student') {
      this.portalRootLabel = 'Student Portal';
      this.portalRootLink = '/student-portal';
    } else {
      this.portalRootLabel = 'Staff Portal';
      this.portalRootLink = '/staff-portal';
    }

    this.notificationService.connect();

    this.updateBreadcrumb();
    this.routerSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.updateBreadcrumb());
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.notificationService.disconnect();
  }

  private updateBreadcrumb(): void {
    this.currentBreadcrumb = this.route.firstChild?.snapshot.data['breadcrumb'] ?? '';
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  onLogoutClick(): void {
    this.showLogoutConfirm = true;
  }

  logout(): void {
    this.showLogoutConfirm = false;
    this.notificationService.disconnect();
    this.authHelper.logout();
    this.router.navigate(['/'], { replaceUrl: true });
  }
}