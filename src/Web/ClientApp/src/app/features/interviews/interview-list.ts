import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { InterviewService } from './interview.service';
import { InterviewDto, InterviewStatus } from './interview.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-interview-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, Card, Button, Badge, Spinner],
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-900">My Interviews</h1>
        <div class="flex gap-2">
          <app-button routerLink="/interviews/schedule">Schedule Interview</app-button>
          <app-button variant="secondary" routerLink="/interviews/availability">Manage Availability</app-button>
        </div>
      </div>

      <!-- Status Tabs -->
      <div class="flex gap-1 mb-4 border-b border-gray-200">
        @for (tab of tabs; track tab.value) {
          <button
            class="px-4 py-2 text-sm font-medium border-b-2 transition-colors"
            [class]="activeTab() === tab.value
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
            (click)="setTab(tab.value)"
          >
            {{ tab.label }}
            @if (tab.value !== 'all') {
              <span class="ml-1 text-xs">({{ getCount(tab.value) }})</span>
            }
          </button>
        }
      </div>

      <app-card>
        @if (loading()) {
          <div class="flex justify-center py-8">
            <app-spinner />
          </div>
        } @else {
          <div class="space-y-3">
            @for (interview of filteredInterviews(); track interview.id) {
              <div class="flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:bg-gray-50">
                <div class="flex-1">
                  <div class="flex items-center gap-3">
                    <span class="font-medium text-gray-900">{{ interview.candidateName }}</span>
                    <app-badge [variant]="typeVariant(interview.type)">{{ interview.type }}</app-badge>
                    <app-badge [variant]="statusVariant(interview.status)">{{ formatStatus(interview.status) }}</app-badge>
                  </div>
                  <p class="text-sm text-gray-500 mt-1">{{ interview.requisitionTitle }}</p>
                  <div class="flex items-center gap-4 mt-1 text-xs text-gray-400">
                    <span>{{ interview.scheduledAt | date:'medium' }}</span>
                    <span>{{ interview.durationMin }} min</span>
                    <span>Interviewer: {{ interview.interviewerName }}</span>
                    @if (interview.meetingLink) {
                      <a [href]="interview.meetingLink" target="_blank" class="text-primary-600 hover:text-primary-700">
                        Meeting Link
                      </a>
                    }
                  </div>
                </div>
                <div class="flex gap-2 ml-4">
                  @if (interview.status === 'scheduled') {
                    <app-button variant="secondary" size="sm" (click)="markCompleted(interview)">
                      Complete
                    </app-button>
                    <app-button variant="secondary" size="sm" (click)="markNoShow(interview)">
                      No Show
                    </app-button>
                    <app-button variant="danger" size="sm" (click)="cancelInterview(interview)">
                      Cancel
                    </app-button>
                  }
                </div>
              </div>
            } @empty {
              <div class="py-8 text-center text-gray-500">
                No {{ activeTab() === 'all' ? '' : activeTab() }} interviews found
              </div>
            }
          </div>
        }
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class InterviewList implements OnInit, OnDestroy {
  private interviewService = inject(InterviewService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  interviews = signal<InterviewDto[]>([]);
  loading = signal(false);
  activeTab = signal<InterviewStatus | 'all'>('all');

  tabs: { label: string; value: InterviewStatus | 'all' }[] = [
    { label: 'All', value: 'all' },
    { label: 'Scheduled', value: 'scheduled' },
    { label: 'Completed', value: 'completed' },
    { label: 'Cancelled', value: 'cancelled' },
    { label: 'No Show', value: 'no_show' },
  ];

  filteredInterviews = signal<InterviewDto[]>([]);

  ngOnInit(): void {
    this.loadInterviews();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setTab(tab: InterviewStatus | 'all'): void {
    this.activeTab.set(tab);
    this.applyFilter();
  }

  getCount(status: InterviewStatus): number {
    return this.interviews().filter((i) => i.status === status).length;
  }

  loadInterviews(): void {
    this.loading.set(true);
    this.interviewService
      .getMyInterviews()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.interviews.set(res);
          this.applyFilter();
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load interviews');
          this.loading.set(false);
        },
      });
  }

  private applyFilter(): void {
    const tab = this.activeTab();
    if (tab === 'all') {
      this.filteredInterviews.set(this.interviews());
    } else {
      this.filteredInterviews.set(this.interviews().filter((i) => i.status === tab));
    }
  }

  markCompleted(interview: InterviewDto): void {
    this.interviewService
      .markCompleted(interview.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Interview marked as completed');
          this.loadInterviews();
        },
        error: () => this.toastService.error('Failed to update interview'),
      });
  }

  markNoShow(interview: InterviewDto): void {
    if (!confirm('Mark this interview as no-show?')) return;
    this.interviewService
      .markNoShow(interview.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Interview marked as no-show');
          this.loadInterviews();
        },
        error: () => this.toastService.error('Failed to update interview'),
      });
  }

  cancelInterview(interview: InterviewDto): void {
    const reason = prompt('Enter cancellation reason:');
    if (!reason) return;
    this.interviewService
      .cancel(interview.id, reason)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Interview cancelled');
          this.loadInterviews();
        },
        error: () => this.toastService.error('Failed to cancel interview'),
      });
  }

  typeVariant(type: string): 'info' | 'success' | 'warning' | 'danger' {
    const map: Record<string, 'info' | 'success' | 'warning' | 'danger'> = {
      phone: 'info',
      video: 'info',
      technical: 'warning',
      onsite: 'success',
      culture: 'success',
    };
    return map[type] ?? 'info';
  }

  statusVariant(status: string): 'info' | 'success' | 'warning' | 'danger' {
    const map: Record<string, 'info' | 'success' | 'warning' | 'danger'> = {
      scheduled: 'info',
      completed: 'success',
      cancelled: 'danger',
      no_show: 'warning',
    };
    return map[status] ?? 'info';
  }

  formatStatus(status: string): string {
    return status.replace('_', ' ');
  }
}
