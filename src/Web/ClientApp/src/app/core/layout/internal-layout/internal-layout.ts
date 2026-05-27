import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../sidebar/sidebar';
import { Header } from '../header/header';
import { Toast } from '../../../shared/ui/toast/toast';

@Component({
  selector: 'app-internal-layout',
  standalone: true,
  imports: [RouterOutlet, Sidebar, Header, Toast],
  template: `
    <div class="flex h-screen overflow-hidden">
      <app-sidebar #sidebar />

      @if (sidebar.mobileOpen()) {
        <div
          class="fixed inset-0 bg-black/50 z-30 lg:hidden"
          (click)="sidebar.mobileOpen.set(false)"
        ></div>
      }

      <div class="flex-1 flex flex-col overflow-hidden lg:ml-64" [class.lg:!ml-16]="sidebar.collapsed()">
        <app-header (menuToggle)="sidebar.toggleMobile()" />

        <main class="flex-1 overflow-y-auto p-6">
          <router-outlet />
        </main>
      </div>

      <app-toast />
    </div>
  `,
  styles: `
    :host { display: contents; }
  `,
})
export class InternalLayout {}
