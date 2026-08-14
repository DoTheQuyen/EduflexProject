import { Injectable } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { BehaviorSubject, interval, Subscription } from 'rxjs';
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

  // Open/actionable record counts per module (Enquiry/Application/Enrolment/Finance) —
  // comes back on the same GET /api/Notifications/summary call as the notification list
  // below, so the sidebar/dashboard badges and the notification bell always refresh
  // together on the same 15s poll instead of firing separate requests.
  private moduleCountsSubject = new BehaviorSubject<Record<string, number>>({});
  moduleCounts$ = this.moduleCountsSubject.asObservable();

  // Redis pub/sub is disabled on the backend for now (see NotificationPublisher/Program.cs),
  // so the SignalR push below never actually receives anything — this interval is what
  // keeps the list current instead. Drop this once live push is restored, or keep it as a
  // belt-and-braces fallback; either is fine.
  private pollSubscription?: Subscription;
  private readonly pollIntervalMs = 15000;

  constructor(
    private authHelper: AuthHelperService,
    private client: Client,
  ) {}

  connect(): void {
    // The backend rejects every notifications/hub call with 403 until the user
    // clears a pending forced password change — don't even try until then, or
    // withAutomaticReconnect() just retries the same failure forever.
    if (this.authHelper.getCurrentUser()?.mustChangePassword) {
      return;
    }

    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    // Load whatever's already outstanding first — this is what makes notifications
    // survive a refresh or "nobody was online when it happened", unlike the live push alone.
    this.loadBacklog();
    this.startPolling();

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiClientUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authHelper.getAuthToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hubConnection.on('ReceiveNotification', (message: RealtimeNotificationMessage) => {
      const current = this.notificationsSubject.value;
      if (current.some((n) => n.id === message.id)) {
        return; // already present (e.g. raced with the initial backlog fetch)
      }
      this.updateList([message, ...current]);
    });

    this.hubConnection
      .start()
      .catch((err) => console.error('Notification hub connection failed', err));
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
    this.stopPolling();
  }

  // Removes one notification from this user's own list — clearing it doesn't affect
  // anyone else with the same role, since it's tracked per-user server-side.
  clear(id: string): void {
    this.client.clear(id).subscribe({
      next: () => this.updateList(this.notificationsSubject.value.filter((n) => n.id !== id)),
      error: (err) => console.error('Failed to clear notification', err),
    });
  }

  private startPolling(): void {
    this.stopPolling(); // guard against a duplicate interval if connect() is ever called twice
    this.pollSubscription = interval(this.pollIntervalMs).subscribe(() => this.loadBacklog());
  }

  private stopPolling(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = undefined;
  }

  private loadBacklog(): void {
    this.client.summary2().subscribe({
      next: (result) => {
        const mapped: RealtimeNotificationMessage[] = (result.notifications ?? []).map((dto) => ({
          id: dto.id ?? '',
          module: dto.module ?? '',
          entityId: dto.entityId ?? '',
          summary: dto.summary ?? '',
          targetType: dto.targetType ?? '',
          targetDepartmentId: dto.targetDepartmentId ?? undefined,
        }));
        this.updateList(mapped);
        this.moduleCountsSubject.next(result.counts ?? {});
      },
      error: (err) => console.error('Failed to load dashboard summary', err),
    });
  }

  private updateList(list: RealtimeNotificationMessage[]): void {
    this.notificationsSubject.next(list);
    // Unread count is now just "how many outstanding" — clearing one removes it from
    // the list directly, so there's no separate read/unread state to track.
    this.unreadCountSubject.next(list.length);
  }
}
