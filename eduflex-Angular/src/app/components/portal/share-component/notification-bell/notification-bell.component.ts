import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { RealtimeNotificationService, RealtimeNotificationMessage } from '../../../../services/realtime-notification.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.css']
})
export class NotificationBellComponent {
  notifications$: Observable<RealtimeNotificationMessage[]>;
  unreadCount$: Observable<number>;
  isOpen = false;

  constructor(private notificationService: RealtimeNotificationService) {
    this.notifications$ = this.notificationService.notifications$;
    this.unreadCount$ = this.notificationService.unreadCount$;
  }

  toggleOpen(): void {
    this.isOpen = !this.isOpen;
  }

  remove(id: string, event: MouseEvent): void {
    event.stopPropagation();
    this.notificationService.clear(id);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('app-notification-bell')) {
      this.isOpen = false;
    }
  }
}