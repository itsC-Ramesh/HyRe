import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ScorecardService } from './scorecard.service';
import { ScorecardDto, Recommendation, ScorecardRatings, SaveDraftPayload, SubmitScorecardPayload } from './scorecard.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

const RATING_DIMENSIONS: { key: keyof ScorecardRatings; label: string }[] = [
  { key: 'technical', label: 'Technical Skills' },
  { key: 'communication', label: 'Communication' },
  { key: 'problemSolving', label: 'Problem Solving' },
  { key: 'cultureFit', label: 'Culture Fit' },
];

const RECOMMENDATION_OPTIONS: { value: Recommendation; label: string }[] = [
  { value: 'StrongYes', label: 'Strong Yes' },
  { value: 'Yes', label: 'Yes' },
  { value: 'No', label: 'No' },
  { value: 'StrongNo', label: 'Strong No' },
];

@Component({
  selector: 'app-scorecard-form',
  standalone: true,
  imports: [FormsModule, Card, Button, Badge, Spinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <div class="flex justify-center py-12">
        <app-spinner />
      </div>
    } @else if (scorecard()) {
      <div class="max-w-3xl mx-auto">
        <div class="flex items-center justify-between mb-6">
          <div>
            <h1 class="text-2xl font-bold text-gray-900">Scorecard</h1>
            <p class="text-sm text-gray-500 mt-1">
              Interviewer: {{ scorecard()!.interviewerId }}
            </p>
          </div>
          @if (scorecard()!.isSubmitted) {
            <app-badge variant="success">Submitted</app-badge>
          } @else {
            <app-badge variant="warning">Draft</app-badge>
          }
        </div>

        @if (scorecard()!.isSubmitted) {
          <!-- Read-only view for submitted scorecards -->
          <div class="space-y-6">
            <app-card title="Ratings">
              <div class="space-y-4">
                @for (dim of dimensions; track dim.key) {
                  <div>
                    <div class="flex items-center justify-between mb-1">
                      <span class="text-sm font-medium text-gray-700">{{ dim.label }}</span>
                      <span class="text-sm font-bold text-gray-900">
                        {{ scorecard()!.ratings[dim.key] ?? '—' }}/5
                      </span>
                    </div>
                    <div class="w-full bg-gray-200 rounded-full h-2">
                      <div
                        class="bg-primary-600 h-2 rounded-full transition-all"
                        [style.width.%]="((scorecard()!.ratings[dim.key] ?? 0) / 5) * 100"
                      ></div>
                    </div>
                  </div>
                }
              </div>
            </app-card>

            <app-card title="Recommendation">
              <app-badge [variant]="recommendationVariant(scorecard()!.recommendation)" class="text-sm">
                {{ recommendationLabel(scorecard()!.recommendation) }}
              </app-badge>
            </app-card>

            <app-card title="Strengths">
              <p class="text-gray-700 whitespace-pre-wrap">{{ scorecard()!.strengths }}</p>
            </app-card>

            <app-card title="Concerns">
              <p class="text-gray-700 whitespace-pre-wrap">{{ scorecard()!.concerns }}</p>
            </app-card>

            @if (scorecard()!.notes) {
              <app-card title="Additional Notes">
                <p class="text-gray-700 whitespace-pre-wrap">{{ scorecard()!.notes }}</p>
              </app-card>
            }

            <div class="flex gap-3">
              <app-button variant="secondary" (click)="goBack()">Back</app-button>
            </div>
          </div>
        } @else {
          <!-- Editable form for draft scorecards -->
          <form (submit)="onSubmit($event)" class="space-y-6">
            <!-- Ratings -->
            <app-card title="Ratings">
              <p class="text-sm text-gray-500 mb-4">Rate each dimension from 1 (poor) to 5 (excellent)</p>
              <div class="space-y-5">
                @for (dim of dimensions; track dim.key) {
                  <div>
                    <div class="flex items-center justify-between mb-2">
                      <label class="text-sm font-medium text-gray-700">{{ dim.label }}</label>
                      <span class="text-sm font-bold text-gray-900">
                        {{ ratings[dim.key] || '—' }}/5
                      </span>
                    </div>
                    <div class="flex gap-1">
                      @for (star of [1, 2, 3, 4, 5]; track star) {
                        <button
                          type="button"
                          (click)="setRating(dim.key, star)"
                          class="p-1 focus:outline-none focus:ring-2 focus:ring-primary-500 rounded"
                          [attr.aria-label]="'Rate ' + dim.label + ' ' + star + ' out of 5'"
                        >
                          <svg
                            class="w-8 h-8 transition-colors"
                            [class.text-yellow-400]="star <= (ratings[dim.key] || 0)"
                            [class.text-gray-300]="star > (ratings[dim.key] || 0)"
                            fill="currentColor"
                            viewBox="0 0 20 20"
                          >
                            <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                          </svg>
                        </button>
                      }
                    </div>
                  </div>
                }
              </div>
            </app-card>

            <!-- Recommendation -->
            <app-card title="Recommendation">
              <div class="flex flex-wrap gap-3">
                @for (opt of recommendationOptions; track opt.value) {
                  <label
                    class="flex items-center gap-2 cursor-pointer rounded-md border px-4 py-2 text-sm transition-colors"
                    [class.border-primary-500]="ratings.recommendation === opt.value"
                    [class.bg-primary-50]="ratings.recommendation === opt.value"
                    [class.border-gray-300]="ratings.recommendation !== opt.value"
                    [class.hover:border-gray-400]="ratings.recommendation !== opt.value"
                  >
                    <input
                      type="radio"
                      name="recommendation"
                      [value]="opt.value"
                      [checked]="ratings.recommendation === opt.value"
                      (change)="setRecommendation(opt.value)"
                      class="sr-only"
                    />
                    {{ opt.label }}
                  </label>
                }
              </div>
            </app-card>

            <!-- Strengths -->
            <app-card title="Strengths">
              <p class="text-sm text-gray-500 mb-2">What are the candidate's key strengths? (required)</p>
              <textarea
                [(ngModel)]="ratings.strengths"
                name="strengths"
                rows="4"
                required
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-primary-500"
                placeholder="Describe the candidate's strengths..."
              ></textarea>
            </app-card>

            <!-- Concerns -->
            <app-card title="Concerns">
              <p class="text-sm text-gray-500 mb-2">What concerns do you have? (required)</p>
              <textarea
                [(ngModel)]="ratings.concerns"
                name="concerns"
                rows="4"
                required
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-primary-500"
                placeholder="Describe any concerns..."
              ></textarea>
            </app-card>

            <!-- Notes -->
            <app-card title="Additional Notes">
              <p class="text-sm text-gray-500 mb-2">Any other feedback? (optional)</p>
              <textarea
                [(ngModel)]="ratings.notes"
                name="notes"
                rows="3"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-primary-500"
                placeholder="Additional notes..."
              ></textarea>
            </app-card>

            <!-- Actions -->
            <div class="flex gap-3 pt-2 pb-8">
              <app-button
                type="submit"
                [loading]="submitting()"
                [disabled]="!canSubmit()"
              >
                Submit Scorecard
              </app-button>
              <app-button
                variant="secondary"
                (click)="saveDraft()"
                [loading]="savingDraft()"
              >
                Save Draft
              </app-button>
              <app-button variant="ghost" (click)="goBack()">
                Cancel
              </app-button>
            </div>
          </form>
        }
      </div>
    }
  `,
  styles: `:host { display: block; }`,
})
export class ScorecardForm implements OnInit, OnDestroy {
  private scorecardService = inject(ScorecardService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  scorecard = signal<ScorecardDto | null>(null);
  loading = signal(false);
  submitting = signal(false);
  savingDraft = signal(false);

  dimensions = RATING_DIMENSIONS;
  recommendationOptions = RECOMMENDATION_OPTIONS;

  ratings: {
    technical: number;
    communication: number;
    problemSolving: number;
    cultureFit: number;
    recommendation: Recommendation | null;
    strengths: string;
    concerns: string;
    notes: string;
  } = {
    technical: 0,
    communication: 0,
    problemSolving: 0,
    cultureFit: 0,
    recommendation: null,
    strengths: '',
    concerns: '',
    notes: '',
  };

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    this.loadScorecard(id);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadScorecard(id: string): void {
    this.loading.set(true);
    this.scorecardService.getByInterview(id).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: (sc) => {
        this.scorecard.set(sc);
        if (!sc.isSubmitted) {
          this.ratings = {
            technical: sc.ratings?.technical ?? 0,
            communication: sc.ratings?.communication ?? 0,
            problemSolving: sc.ratings?.problemSolving ?? 0,
            cultureFit: sc.ratings?.cultureFit ?? 0,
            recommendation: sc.recommendation ?? null,
            strengths: sc.strengths ?? '',
            concerns: sc.concerns ?? '',
            notes: sc.notes ?? '',
          };
        }
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load scorecard');
        this.router.navigate(['/scorecards/my']);
      },
    });
  }

  setRating(key: keyof ScorecardRatings, value: number): void {
    (this.ratings as Record<string, unknown>)[key] = value;
  }

  setRecommendation(value: Recommendation): void {
    this.ratings.recommendation = value;
  }

  canSubmit(): boolean {
    return (
      this.ratings.technical > 0 &&
      this.ratings.communication > 0 &&
      this.ratings.problemSolving > 0 &&
      this.ratings.cultureFit > 0 &&
      this.ratings.recommendation !== null &&
      this.ratings.strengths.trim().length > 0 &&
      this.ratings.concerns.trim().length > 0
    );
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    const sc = this.scorecard();
    if (!sc) return;

    this.submitting.set(true);
    const payload: SubmitScorecardPayload = {
      ratings: {
        technical: this.ratings.technical,
        communication: this.ratings.communication,
        problemSolving: this.ratings.problemSolving,
        cultureFit: this.ratings.cultureFit,
      },
      recommendation: this.ratings.recommendation!,
      strengths: this.ratings.strengths,
      concerns: this.ratings.concerns,
      notes: this.ratings.notes || null,
    };

    this.scorecardService.submit(sc.id, payload).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: () => {
        this.toastService.success('Scorecard submitted');
        this.router.navigate(['/scorecards/my']);
      },
      error: () => {
        this.toastService.error('Failed to submit scorecard');
        this.submitting.set(false);
      },
    });
  }

  saveDraft(): void {
    const sc = this.scorecard();
    if (!sc) return;

    this.savingDraft.set(true);
    const payload: SaveDraftPayload = {
      ratings: {
        technical: this.ratings.technical,
        communication: this.ratings.communication,
        problemSolving: this.ratings.problemSolving,
        cultureFit: this.ratings.cultureFit,
      },
      recommendation: this.ratings.recommendation,
      strengths: this.ratings.strengths || null,
      concerns: this.ratings.concerns || null,
      notes: this.ratings.notes || null,
    };

    this.scorecardService.saveDraft(sc.id, payload).pipe(
      takeUntil(this.destroy$),
    ).subscribe({
      next: () => {
        this.toastService.success('Draft saved');
        this.savingDraft.set(false);
      },
      error: () => {
        this.toastService.error('Failed to save draft');
        this.savingDraft.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/scorecards/my']);
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
