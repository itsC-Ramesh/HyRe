import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { Toast } from '../../../shared/ui/toast/toast';

@Component({
  selector: 'app-candidate-layout',
  standalone: true,
  imports: [RouterOutlet, Toast],
  template: `
    <div class="flex flex-col h-screen">
      <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
        <span class="text-xl font-bold text-primary-600">HyRe</span>
        <div class="flex items-center gap-4">
          <span class="text-sm text-gray-600">Application Status</span>
          <button
            class="text-sm text-gray-500 hover:text-gray-700"
            (click)="logout()"
          >
            Sign out
          </button>
        </div>
      </header>

      <main class="flex-1 overflow-y-auto p-6">
        <router-outlet />
      </main>

      <app-toast />
    </div>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class CandidateLayout {
  private authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }
}
