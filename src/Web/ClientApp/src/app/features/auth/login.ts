import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, Card, Button],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4">
      <div class="max-w-md w-full">
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold text-primary-600">HyRe</h1>
          <p class="mt-2 text-sm text-gray-600">Sign in to your account</p>
        </div>

        <app-card>
          <form (submit)="onSubmit($event)" class="space-y-4">
            @if (serverError()) {
              <div class="rounded-md bg-danger-50 p-4">
                <p class="text-sm text-danger-700">{{ serverError() }}</p>
              </div>
            }

            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium text-gray-700">
                Email <span class="text-danger-500">*</span>
              </label>
              <input
                type="email"
                placeholder="you&#64;company.com"
                [ngModel]="email()"
                (ngModelChange)="email.set($event)"
                name="email"
                class="block w-full rounded-md border px-3 py-2 text-sm shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-0 border-gray-300 focus:border-primary-500 focus:ring-primary-500"
              />
            </div>

            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium text-gray-700">
                Password <span class="text-danger-500">*</span>
              </label>
              <input
                type="password"
                placeholder="Enter your password"
                [ngModel]="password()"
                (ngModelChange)="password.set($event)"
                name="password"
                class="block w-full rounded-md border px-3 py-2 text-sm shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-0 border-gray-300 focus:border-primary-500 focus:ring-primary-500"
              />
            </div>

            <app-button
              type="submit"
              [loading]="loading()"
              [disabled]="loading()"
            >
              Sign in
            </app-button>
          </form>
        </app-card>
      </div>
    </div>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = signal('');
  password = signal('');
  serverError = signal('');
  loading = signal(false);

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.serverError.set('');

    if (!this.email() || !this.password()) {
      this.serverError.set('Email and password are required');
      return;
    }

    this.loading.set(true);
    try {
      await this.authService.login(this.email(), this.password());
      this.router.navigate(['/dashboard']);
    } catch (e: unknown) {
      this.serverError.set(e instanceof Error ? e.message : 'Login failed');
    } finally {
      this.loading.set(false);
    }
  }
}
