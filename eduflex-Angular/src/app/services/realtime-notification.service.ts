import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { AuthHelperService } from './auth-helper.service';
import { Client } from './api.services';
import { environment } from '../environments/environment';

// SignalR's default JSON hub protocol camelCases property names, so this mirrors the
// backend's NotificationMessage record in camelCase, not PascalCase. targetType is one
// of 'Department' | 'DepartmentHead' | 'Staff' — see NotificationTargetType on the backend.
export interface RealtimeNotificationMessage {
  id: string;
  module: string;
  entityId: string;
  summary: string;
  targetType: string;
  targetDepartmentId?: string;
}

@Injectable({ providedIn: 'root' })
export class RealtimeNotificationService {
  private hubConnection?: HubConnection;

  private notificationsSubject = new BehaviorSubject<RealtimeNotificationMessage[]>([]);
  notifications$ = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSubject.asObservable();

  constructor(private authHelper: AuthHelperService, private client: Client) {}

  connect(): void {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    // Load whatever's already outstanding first — this is what makes notifications
    // survive a refresh or "nobody was online when it happened", unlike the live push alone.
    this.loadBacklog();

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiClientUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authHelper.getAuthToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hubConnection.on('ReceiveNotification', (message: RealtimeNotificationMessage) => {
      const current = this.notificationsSubject.value;
      if (current.some(n => n.id === message.id)) {
        return; // already present (e.g. raced with the initial backlog fetch)
      }
      this.updateList([message, ...current]);
    });

    this.hubConnection.start().catch(err => console.error('Notification hub connection failed', err));
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  // Removes one notification from this user's own list — clearing it doesn't affect
  // anyone else with the same role, since it's tracked per-user server-side.
  clear(id: string): void {
    this.client.clear(id).subscribe({
      next: () => this.updateList(this.notificationsSubject.value.filter(n => n.id !== id)),
      error: err => console.error('Failed to clear notification', err)
    });
  }

  private loadBacklog(): void {
    this.client.notifications().subscribe({
      next: dtos => {
        // TODO(department-migration): drop the `as any` once `nswag run` has been
        // re-run against the updated backend — NotificationDto doesn't declare
        // targetType/targetDepartmentId yet because api.services.ts hasn't been
        // regenerated, even though the server already sends them.
        const mapped: RealtimeNotificationMessage[] = dtos.map(dto => ({
          id: dto.id ?? '',
          module: dto.module ?? '',
          entityId: dto.entityId ?? '',
          summary: dto.summary ?? '',
          targetType: (dto as any).targetType ?? '',
          targetDepartmentId: (dto as any).targetDepartmentId ?? undefined
        }));
        this.updateList(mapped);
      },
      error: err => console.error('Failed to load notifications', err)
    });
  }

  private updateList(list: RealtimeNotificationMessage[]): void {
    this.notificationsSubject.next(list);
    // Unread count is now just "how many outstanding" — clearing one removes it from
    // the list directly, so there's no separate read/unread state to track.
    this.unreadCountSubject.next(list.length);
  }
}