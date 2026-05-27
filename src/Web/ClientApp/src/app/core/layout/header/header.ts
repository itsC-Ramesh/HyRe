import { Component, inject, signal, computed, output } from '@angular/core';
import { AuthService } from '../../auth/auth.service';
import { NotificationBell } from '../../notifications/notification-bell';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [NotificationBell],
  template: `
    <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 lg:px-6">
      <div class="flex items-center gap-4">
        <button
          class="lg:hidden p-2 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-100"
          (click)="onMenuToggle()"
        >
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
      </div>

      <div class="flex items-center gap-4">
        <app-notification-bell />

        <div class="relative">
          <button
            class="flex items-center gap-2 p-2 rounded-md text-gray-600 hover:text-gray-900 hover:bg-gray-100"
            (click)="toggleUserMenu()"
          >
            <div class="h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
              <span class="text-sm font-medium text-primary-700">{{ userInitial() }}</span>
            </div>
            <span class="hidden sm:block text-sm font-medium">{{ userName() }}</span>
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          @if (userMenuOpen()) {
            <div class="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg border border-gray-200 py-1 z-50">
              <button
                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                (click)="logout()"
              >
                Sign out
              </button>
            </div>
          }
        </div>
      </div>
    </header>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class Header {
  private authService = inject(AuthService);

  menuToggle = output<void>();

  userMenuOpen = signal(false);

  userName = computed(() => this.authService.user()?.email ?? 'User');
  userInitial = computed(() => {
    const email = this.authService.user()?.email ?? 'U';
    return email.charAt(0).toUpperCase();
  });

  onMenuToggle(): void {
    this.menuToggle.emit();
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((o) => !o);
  }

  logout(): void {
    this.userMenuOpen.set(false);
    this.authService.logout();
  }
}
