import { Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  template: `
    @if (fullPage()) {
      <div class="fixed inset-0 flex items-center justify-center bg-white/80 z-50">
        <div class="animate-spin rounded-full h-8 w-8 border-4 border-primary-200 border-t-primary-600"></div>
      </div>
    } @else {
      <div class="animate-spin rounded-full h-5 w-5 border-2 border-primary-200 border-t-primary-600"></div>
    }
  `,
})
export class Spinner {
  fullPage = input(false);
}
