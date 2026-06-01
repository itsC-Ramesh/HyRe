import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200">
      @if (title()) {
        <div class="px-6 py-4 border-b border-gray-200">
          <h3 class="text-lg font-medium text-gray-900">{{ title() }}</h3>
        </div>
      }
      <div class="px-6 py-4">
        <ng-content />
      </div>
    </div>
  `,
})
export class Card {
  title = input('');
}
