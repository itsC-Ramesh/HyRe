import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ScorecardService } from './scorecard.service';
import { ScorecardDto, PaginatedScorecards } from './scorecard.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-my-scorecards',
  standalone: true,
  imports: [DatePipe, Card, Button, Badge, Spinner, EmptyState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-900">My Scorecards</h1>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-12">
          <app-spinner />
        </div>
      } @else {
        <!-- Pending Scorecards -->
        <div class="mb-8">
          <h2 class="text-lg font-semibold text-gray-800 mb-3">
            Pending
            @if (pending().length > 0) {
              <app-badge variant="warning">{{ pending().length }}</app-badge>
            }
          </h2>
          @if (pending().length > 0) {
            <div class="space-y-3">
              @for (sc of pending(); track sc.id) {
                <app-card>
                  <div class="flex items-center justify-between">
                    <div>
                      <p class="text-sm font-medium text-gray-900">
                        Scorecard for Interview
                      </p>
                      <p class="text-xs text-gray-500 mt-1">
                        Interview ID: {{ sc.interviewId }}
                      </p>
                    </div>
                    <app-button size="sm" (click)="openScorecard(sc.interviewId)">
                      Fill Scorecard
                    </app-button>
                  </div>
                </app-card>
              }
            </div>
          } @else {
            <app-empty-state title="No pending scorecards" message="You have no scorecards waiting to be filled." />
          }
        </div>

        <!-- Submitted Scorecards -->
        <div>
          <h2 class="text-lg font-semibold text-gray-800 mb-3">
            Submitted
            @if (submitted().length > 0) {
              <app-badge variant="success">{{ submitted().length }}</app-badge>
            }
          </h2>
          @if (submitted().length > 0) {
            <div class="space-y-3">
              @for (sc of submitted(); track sc.id) {
                <app-card>
                  <div class="flex items-center justify-between">
                    <div>
                      <div class="flex items-center gap-2">
                        <p class="text-sm font-medium text-gray-900">
                          Scorecard for Interview
                        </p>
                        <app-badge [variant]="recommendationVariant(sc.recommendation)">
                          {{ recommendationLabel(sc.recommendation) }}
                        </app-badge>
                      </div>
                      <p class="text-xs text-gray-500 mt-1">
                        Submitted {{ sc.submittedAt | date:'medium' }}
                      </p>
                    </div>
                    <app-button variant="secondary" size="sm" (click)="openScorecard(sc.interviewId)">
                      View
                    </app-button>
                  </div>
                </app-card>
              }
            </div>
          } @else {
            <app-empty-state title="No submitted scorecards" message="You haven't submitted any scorecards yet." />
          }
        </div>

        <!-- Pagination -->
        @if (data() && data()!.totalPages > 1) {
          <div class="flex items-center justify-between mt-6 pt-4 border-t border-gray-200">
            <span class="text-sm text-gray-500">
              Page {{ data()!.page }} of {{ data()!.totalPages }}
              ({{ data()!.totalCount }} total)
            </span>
            <div class="flex gap-2">
              <app-button
                variant="secondary"
                size="sm"
                [disabled]="data()!.page <= 1"
                (click)="goToPage(data()!.page - 1)"
              >Previous</app-button>
              <app-button
                variant="secondary"
                size="sm"
                [disabled]="data()!.page >= data()!.totalPages"
                (click)="goToPage(data()!.page + 1)"
              >Next</app-button>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class MyScorecards implements OnInit, OnDestroy {
  private scorecardService = inject(ScorecardService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  data = signal<PaginatedScorecards | null>(null);
  loading = signal(false);
  pending = signal<ScorecardDto[]>([]);
  submitted = signal<ScorecardDto[]>([]);
  currentPage = 1;

  ngOnInit(): void {
    this.loadScorecards();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadScorecards(): void {
    this.loading.set(true);
    this.scorecardService.getMy(this.currentPage).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: (res) => {
        this.data.set(res);
        this.pending.set(res.items.filter((s) => !s.isSubmitted));
        this.submitted.set(res.items.filter((s) => s.isSubmitted));
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load scorecards');
        this.loading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadScorecards();
  }

  openScorecard(id: string): void {
    this.router.navigate(['/scorecards', id]);
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
