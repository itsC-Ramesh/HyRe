import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="flex flex-col gap-1">
      @if (label()) {
        <label class="text-sm font-medium text-gray-700">
          {{ label() }}
          @if (required()) {
            <span class="text-danger-500">*</span>
          }
        </label>
      }
      <input
        [type]="type()"
        [placeholder]="placeholder()"
        [disabled]="disabled()"
        [class]="inputClasses()"
        [ngModel]="value()"
        (ngModelChange)="value.set($event); valueChange.emit($event)"
      />
      @if (error()) {
        <p class="text-sm text-danger-600">{{ error() }}</p>
      }
    </div>
  `,
})
export class Input {
  label = input('');
  type = input('text');
  placeholder = input('');
  disabled = input(false);
  required = input(false);
  error = input('');
  value = signal('');
  valueChange = output<string>();

  inputClasses(): string {
    const base = 'block w-full rounded-md border px-3 py-2 text-sm shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-0';
    const normal = 'border-gray-300 focus:border-primary-500 focus:ring-primary-500';
    const error = 'border-danger-300 focus:border-danger-500 focus:ring-danger-500';
    return `${base} ${this.error() ? error : normal}`;
  }
}
