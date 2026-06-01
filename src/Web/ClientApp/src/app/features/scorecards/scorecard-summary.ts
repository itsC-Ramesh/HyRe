import { Component, inject, signal, input, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ScorecardService } from './scorecard.service';
import { ScorecardSummaryDto, ScorecardDto } from './scorecard.models';
import { Card } from '../../shared/ui/card/card';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { ToastService } from '../../shared/ui/toast/toast.service';

const RATING_LABELS: Record<string, string> = {
  technical: 'Technical Skills',
  communication: 'Communication',
  problemSolving: 'Problem Solving',
  cultureFit: 'Culture Fit',
};

@Component({
  selector: 'app-scorecard-summary',
  standalone: true,
  imports: [DatePipe, DecimalPipe, Card, Badge, Spinner, EmptyState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <div class="flex justify-center py-8">
        <app-spinner />
      </div>
    } @else if (summary()) {
      <div class="space-y-6">
        <!-- Overview Stats -->
        <div class="grid grid-cols-3 gap-4">
          <div class="bg-gray-50 rounded-lg p-4 text-center">
            <p class="text-2xl font-bold text-gray-900">{{ summary()!.totalInterviews }}</p>
            <p class="text-sm text-gray-500">Total Interviews</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4 text-center">
            <p class="text-2xl font-bold text-success-600">{{ summary()!.submittedCount }}</p>
            <p class="text-sm text-gray-500">Submitted</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4 text-center">
            <p class="text-2xl font-bold text-warning-600">{{ summary()!.pendingCount }}</p>
            <p class="text-sm text-gray-500">Pending</p>
          </div>
        </div>

        <!-- Average Ratings -->
        @if (summary()!.submittedCount > 0) {
          <app-card title="Average Ratings">
            <div class="space-y-4">
              @for (dim of ratingDimensions(); track dim.key) {
                <div>
                  <div class="flex items-center justify-between mb-1">
                    <span class="text-sm font-medium text-gray-700">{{ dim.label }}</span>
                    <span class="text-sm font-bold text-gray-900">
                      {{ dim.value | number:'1.1-1' }}/5
                    </span>
                  </div>
                  <div class="w-full bg-gray-200 rounded-full h-2.5">
                    <div
                      class="bg-primary-600 h-2.5 rounded-full transition-all"
                      [style.width.%]="(dim.value / 5) * 100"
                    ></div>
                  </div>
                </div>
              }
            </div>
          </app-card>

          <!-- Recommendation Distribution -->
          <app-card title="Recommendation Distribution">
            <div class="space-y-3">
              @for (rec of recommendationDistribution(); track rec.label) {
                <div class="flex items-center gap-3">
                  <span class="text-sm text-gray-700 w-24">{{ rec.label }}</span>
                  <div class="flex-1 bg-gray-200 rounded-full h-4 overflow-hidden">
                    <div
                      class="h-4 rounded-full transition-all"
                      [class.bg-success-500]="rec.variant === 'success'"
                      [class.bg-danger-500]="rec.variant === 'danger'"
                      [class.bg-gray-400]="rec.variant === 'info'"
                      [style.width.%]="rec.percentage"
                    ></div>
                  </div>
                  <span class="text-sm font-medium text-gray-900 w-8 text-right">{{ rec.count }}</span>
                </div>
              }
            </div>
          </app-card>
        }

        <!-- Individual Scorecards -->
        @if (scorecards().length > 0) {
          <app-card title="Individual Scorecards">
            <div class="space-y-3">
              @for (sc of scorecards(); track sc.id) {
                <div class="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
                  <div>
                    <p class="text-sm font-medium text-gray-900">
                      Interviewer: {{ sc.interviewerId }}
                    </p>
                    @if (sc.submittedAt) {
                      <p class="text-xs text-gray-500">
                        {{ sc.submittedAt | date:'medium' }}
                      </p>
                    }
                  </div>
                  <app-badge [variant]="recommendationVariant(sc.recommendation)">
                    {{ recommendationLabel(sc.recommendation) }}
                  </app-badge>
                </div>
              }
            </div>
          </app-card>
        }
      </div>
    } @else {
      <app-empty-state title="No summary available" message="Could not load scorecard summary." />
    }
  `,
  styles: `:host { display: block; }`,
})
export class ScorecardSummary implements OnInit, OnDestroy {
  private scorecardService = inject(ScorecardService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  applicationId = input.required<string>();

  summary = signal<ScorecardSummaryDto | null>(null);
  scorecards = signal<ScorecardDto[]>([]);
  loading = signal(false);
  ratingDimensions = signal<{ key: string; label: string; value: number }[]>([]);
  recommendationDistribution = signal<{ label: string; count: number; percentage: number; variant: string }[]>([]);

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loading.set(true);
    const appId = this.applicationId();

    this.scorecardService.getSummary(appId).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.buildRatingDimensions(summary);
        this.buildRecommendationDistribution(summary);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load scorecard summary');
        this.loading.set(false);
      },
    });

    this.scorecardService.getByApplication(appId).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: (scorecards) => this.scorecards.set(scorecards),
      error: () => {},
    });
  }

  private buildRatingDimensions(summary: ScorecardSummaryDto): void {
    const dims = Object.entries(summary.averageRatings).map(([key, value]) => ({
      key,
      label: RATING_LABELS[key] ?? key,
      value,
    }));
    this.ratingDimensions.set(dims);
  }

  private buildRecommendationDistribution(summary: ScorecardSummaryDto): void {
    const total = summary.submittedCount;
    const recLabels: Record<string, string> = {
      StrongYes: 'Strong Yes',
      Yes: 'Yes',
      No: 'No',
      StrongNo: 'Strong No',
    };
    const recVariants: Record<string, string> = {
      StrongYes: 'success',
      Yes: 'success',
      No: 'danger',
      StrongNo: 'danger',
    };

    const dist = Object.entries(summary.recommendationBreakdown).map(([key, count]) => ({
      label: recLabels[key] ?? key,
      count,
      percentage: total > 0 ? (count / total) * 100 : 0,
      variant: recVariants[key] ?? 'info',
    }));
    this.recommendationDistribution.set(dist);
  }

  recommendationLabel(rec: string): string {
    const map: Record<string, string> = {
      StrongYes: 'Strong Yes',
      Yes: 'Yes',
      No: 'No',
      StrongNo: 'Strong No',
    };
    return map[rec] ?? rec;
  }

  recommendationVariant(rec: string): 'success' | 'warning' | 'danger' | 'info' {
    const map: Record<string, 'success' | 'warning' | 'danger' | 'info'> = {
      StrongYes: 'success',
      Yes: 'success',
      No: 'danger',
      StrongNo: 'danger',
    };
    return map[rec] ?? 'info';
  }
}
