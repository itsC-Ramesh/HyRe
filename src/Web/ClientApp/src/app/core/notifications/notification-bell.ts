import { Component, ChangeDetectionStrategy, ElementRef, inject, signal } from '@angular/core';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:click)': 'onDocumentClick($event)',
  },
  template: `
    <div class="relative">
      <button
        class="relative p-2 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-100"
        (click)="toggleDropdown()"
      >
        <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round"
            d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
        </svg>
        @if (notifService.unreadCount() > 0) {
          <span class="absolute -top-0.5 -right-0.5 h-5 w-5 rounded-full bg-danger-500 text-white text-xs flex items-center justify-center">
            {{ notifService.unreadCount() > 9 ? '9+' : notifService.unreadCount() }}
          </span>
        }
      </button>

      @if (isOpen()) {
        <div class="absolute right-0 mt-2 w-80 bg-white rounded-md shadow-lg border border-gray-200 z-50">
          <div class="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
            <h4 class="text-sm font-semibold text-gray-900">Notifications</h4>
            @if (notifService.unreadCount() > 0) {
              <button
                class="text-xs text-primary-600 hover:text-primary-700"
                (click)="markAllRead()"
              >
                Mark all read
              </button>
            }
          </div>

          <div class="max-h-80 overflow-y-auto">
            @if (notifService.notifications().length === 0) {
              <p class="px-4 py-6 text-sm text-gray-500 text-center">No notifications</p>
            } @else {
              @for (notif of notifService.notifications(); track notif.id) {
                <div
                  class="px-4 py-3 border-b border-gray-100 hover:bg-gray-50 cursor-pointer"
                  [class.bg-blue-50]="!notif.readAt"
                  (click)="notifService.markAsRead(notif.id)"
                >
                  <p class="text-sm text-gray-800">{{ formatNotification(notif) }}</p>
                  <p class="text-xs text-gray-400 mt-1">{{ timeAgo(notif.createdAt) }}</p>
                </div>
              }
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class NotificationBell {
  notifService = inject(NotificationService);
  private el = inject(ElementRef);
  isOpen = signal(false);

  toggleDropdown(): void {
    this.isOpen.update((o) => !o);
    if (this.isOpen()) {
      this.notifService.fetchNotifications();
    }
  }

  onDocumentClick(event: MouseEvent): void {
    if (this.isOpen() && !this.el.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  markAllRead(): void {
    this.notifService.markAllAsRead();
  }

  formatNotification(notif: { type: string; payloadJson: string }): string {
    try {
      const payload = JSON.parse(notif.payloadJson);
      return payload.message ?? notif.type;
    } catch {
      return notif.type;
    }
  }

  timeAgo(dateStr: string): string {
    const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (seconds < 60) return 'just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    return `${Math.floor(seconds / 86400)}d ago`;
  }
}
