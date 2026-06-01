import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { Card } from '../../shared/ui/card/card';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [Card],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">Dashboard</h1>

      <app-card title="Welcome">
        <p class="text-gray-600">
          Hello, <span class="font-medium text-gray-900">{{ userName() }}</span>!
          You are logged in as <span class="font-medium text-primary-600">{{ userRoles() }}</span>.
        </p>
        <p class="text-gray-500 mt-2 text-sm">
          Feature modules will be built here. Use the sidebar to navigate.
        </p>
      </app-card>
    </div>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class Dashboard {
  private authService = inject(AuthService);

  userName = computed(() => this.authService.user()?.email ?? 'User');
  userRoles = computed(() => this.authService.user()?.roles?.join(', ') ?? 'Unknown');
}
