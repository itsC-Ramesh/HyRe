import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="fixed top-4 right-4 z-50 flex flex-col gap-2">
      @for (toast of toastService.toasts(); track toast.id) {
        <div [class]="toastClasses(toast.type)" (click)="toastService.dismiss(toast.id)">
          <span>{{ toast.message }}</span>
        </div>
      }
    </div>
  `,
})
export class Toast {
  toastService = inject(ToastService);

  toastClasses(type: string): string {
    const base = 'px-4 py-3 rounded-md shadow-lg text-sm font-medium cursor-pointer transition-all';
    const types: Record<string, string> = {
      success: 'bg-success-600 text-white',
      error: 'bg-danger-600 text-white',
      info: 'bg-primary-600 text-white',
    };
    return `${base} ${types[type] ?? types['info']}`;
  }
}
