import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import { Subscription, interval, switchMap, filter } from 'rxjs';
import { ApiService } from '../http/api.service';
import { AuthState } from '../auth/auth.state';

export interface AppNotification {
  id: string;
  type: string;
  payloadJson: string;
  readAt: string | null;
  createdAt: string;
}

interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private api = inject(ApiService);
  private authState = inject(AuthState);
  private pollSub?: Subscription;

  readonly unreadCount = signal(0);
  readonly notifications = signal<AppNotification[]>([]);

  startPolling(): void {
    this.stopPolling();
    this.fetchUnreadCount();

    this.pollSub = interval(30000)
      .pipe(
        filter(() => this.authState.isAuthenticated()),
        switchMap(() => {
          this.fetchUnreadCount();
          return [];
        })
      )
      .subscribe();
  }

  stopPolling(): void {
    this.pollSub?.unsubscribe();
  }

  fetchUnreadCount(): void {
    this.api.get<{ count: number }>('/Notifications/unread-count').subscribe({
      next: (res) => this.unreadCount.set(res.count),
      error: () => {},
    });
  }

  fetchNotifications(page = 1, pageSize = 10): void {
    this.api
      .get<PaginatedList<AppNotification>>(`/Notifications?page=${page}&pageSize=${pageSize}`)
      .subscribe({
        next: (res) => this.notifications.set(res.items),
        error: () => {},
      });
  }

  markAsRead(id: string): void {
    this.api.post<void>(`/Notifications/${id}/read`, {}).subscribe({
      next: () => {
        this.notifications.update((items) =>
          items.map((n) => (n.id === id ? { ...n, readAt: new Date().toISOString() } : n))
        );
        this.unreadCount.update((c) => Math.max(0, c - 1));
      },
      error: () => {},
    });
  }

  markAllAsRead(): void {
    const unread = this.notifications().filter((n) => !n.readAt);
    unread.forEach((n) => this.markAsRead(n.id));
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}
