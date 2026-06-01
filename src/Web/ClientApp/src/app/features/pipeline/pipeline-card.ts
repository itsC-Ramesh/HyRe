import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CdkDrag } from '@angular/cdk/drag-drop';
import { RouterLink } from '@angular/router';
import { PipelineApplicationCard } from './pipeline.models';

@Component({
  selector: 'app-pipeline-card',
  standalone: true,
  imports: [CdkDrag, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      cdkDrag
      class="bg-white rounded-lg border border-gray-200 p-3 mb-2 shadow-sm hover:shadow-md transition-shadow cursor-grab"
      [cdkDragData]="application()"
    >
      <div class="flex items-start justify-between">
        <div class="flex-1 min-w-0">
          <a
            [routerLink]="['/candidates', application().candidateId]"
            class="text-sm font-medium text-gray-900 hover:text-primary-600 truncate block"
            (click)="$event.stopPropagation()"
          >
            {{ application().candidateName }}
          </a>
          <p class="text-xs text-gray-500 truncate mt-0.5">{{ application().candidateEmail }}</p>
        </div>
        <span
          [class]="daysBadgeClass()"
          class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ml-2 shrink-0"
        >
          {{ application().daysInStage }}d
        </span>
      </div>
    </div>
  `,
  styles: `
    :host { display: block; }
    .cdk-drag-preview {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      border-radius: 0.5rem;
    }
    .cdk-drag-placeholder {
      opacity: 0.3;
    }
    .cdk-drag-animating {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
  `,
})
export class PipelineCard {
  application = input.required<PipelineApplicationCard>();

  daysBadgeClass(): string {
    const days = this.application().daysInStage;
    if (days > 14) return 'bg-danger-100 text-danger-700';
    if (days > 7) return 'bg-warning-100 text-warning-700';
    return 'bg-gray-100 text-gray-600';
  }
}
