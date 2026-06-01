import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { NAV_ITEMS } from '../nav-items';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside [class]="sidebarClasses()">
      <div class="flex items-center h-16 px-4 border-b border-gray-200">
        <span class="text-xl font-bold text-primary-600">HyRe</span>
      </div>

      <nav class="mt-4 flex-1 overflow-y-auto">
        @for (item of visibleItems(); track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="bg-primary-50 text-primary-600 border-primary-600"
            [class]="navItemClasses()"
          >
            <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
              <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="item.icon" />
            </svg>
            @if (!collapsed()) {
              <span class="ml-3">{{ item.label }}</span>
            }
          </a>
        }
      </nav>
    </aside>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class Sidebar {
  private authService = inject(AuthService);

  collapsed = signal(false);
  mobileOpen = signal(false);

  visibleItems = computed(() =>
    NAV_ITEMS.filter((item) => this.authService.hasPermission(item.permission))
  );

  toggleCollapse(): void {
    this.collapsed.update((c) => !c);
  }

  toggleMobile(): void {
    this.mobileOpen.update((o) => !o);
  }

  sidebarClasses(): string {
    const base = 'fixed top-0 left-0 h-full bg-white border-r border-gray-200 z-40 transition-all duration-300 flex flex-col';
    const width = this.collapsed() ? 'w-16' : 'w-64';
    const mobile = this.mobileOpen() ? 'translate-x-0' : '-translate-x-full lg:translate-x-0';
    return `${base} ${width} ${mobile}`;
  }

  navItemClasses(): string {
    return 'flex items-center px-4 py-2.5 text-sm font-medium text-gray-600 hover:bg-gray-50 hover:text-gray-900 border-l-2 border-transparent transition-colors';
  }
}
