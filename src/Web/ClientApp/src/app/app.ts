import { Component, inject, effect } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './core/notifications/notification.service';
import { AuthState } from './core/auth/auth.state';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class App {
  private notificationService = inject(NotificationService);
  private authState = inject(AuthState);

  constructor() {
    effect(() => {
      if (this.authState.isAuthenticated()) {
        this.notificationService.startPolling();
      } else {
        this.notificationService.stopPolling();
      }
    });
  }
}
