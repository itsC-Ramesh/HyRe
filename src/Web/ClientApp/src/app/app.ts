import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './core/notifications/notification.service';
import { AuthState } from './core/auth/auth.state';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class App implements OnInit, OnDestroy {
  private notificationService = inject(NotificationService);
  private authState = inject(AuthState);

  ngOnInit(): void {
    if (this.authState.isAuthenticated()) {
      this.notificationService.startPolling();
    }
  }

  ngOnDestroy(): void {
    this.notificationService.stopPolling();
  }
}
