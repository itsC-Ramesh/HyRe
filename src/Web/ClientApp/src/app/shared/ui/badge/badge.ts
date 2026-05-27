import { Component, input } from '@angular/core';

@Component({
  selector: 'app-badge',
  standalone: true,
  template: `
    <span [class]="badgeClasses()">
      <ng-content />
    </span>
  `,
})
export class Badge {
  variant = input<'info' | 'success' | 'warning' | 'danger'>('info');

  badgeClasses(): string {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const variants: Record<string, string> = {
      info: 'bg-primary-100 text-primary-800',
      success: 'bg-success-100 text-success-800',
      warning: 'bg-warning-100 text-warning-800',
      danger: 'bg-danger-100 text-danger-800',
    };
    return `${base} ${variants[this.variant()]}`;
  }
}
