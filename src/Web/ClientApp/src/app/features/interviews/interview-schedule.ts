import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { InterviewService } from './interview.service';
import { InterviewType } from './interview.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-interview-schedule',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Card, Button],
  template: `
    <div class="max-w-3xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">Schedule Interview</h1>

      <app-card>
        <form (submit)="onSubmit($event)" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Application ID *</label>
            <input
              type="text"
              [(ngModel)]="form.applicationId"
              name="applicationId"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="Enter application ID"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Interviewer ID *</label>
            <input
              type="text"
              [(ngModel)]="form.interviewerId"
              name="interviewerId"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="Enter interviewer user ID"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Interview Type *</label>
            <select
              [(ngModel)]="form.type"
              name="type"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            >
              <option value="">Select type...</option>
              @for (t of interviewTypes; track t.value) {
                <option [value]="t.value">{{ t.label }}</option>
              }
            </select>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Date & Time *</label>
              <input
                type="datetime-local"
                [(ngModel)]="form.scheduledAt"
                name="scheduledAt"
                required
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Duration (minutes) *</label>
              <input
                type="number"
                [(ngModel)]="form.durationMin"
                name="durationMin"
                required
                min="15"
                max="480"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                placeholder="60"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Meeting Link</label>
            <input
              type="url"
              [(ngModel)]="form.meetingLink"
              name="meetingLink"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="https://meet.google.com/..."
            />
          </div>

          <div class="flex gap-3 pt-4">
            <app-button type="submit" [loading]="loading()">
              Schedule Interview
            </app-button>
            <app-button variant="secondary" (click)="router.navigate(['/interviews'])">
              Cancel
            </app-button>
          </div>
        </form>
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class InterviewSchedule implements OnInit, OnDestroy {
  private interviewService = inject(InterviewService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();
  router = inject(Router);

  loading = signal(false);

  interviewTypes: { label: string; value: InterviewType }[] = [
    { label: 'Phone Screen', value: 'phone' },
    { label: 'Video Call', value: 'video' },
    { label: 'Technical', value: 'technical' },
    { label: 'Onsite', value: 'onsite' },
    { label: 'Culture Fit', value: 'culture' },
  ];

  form = {
    applicationId: '',
    interviewerId: '',
    type: '' as InterviewType | '',
    scheduledAt: '',
    durationMin: 60,
    meetingLink: '',
  };

  ngOnInit(): void {
    const applicationId = this.route.snapshot.queryParams['applicationId'];
    if (applicationId) {
      this.form.applicationId = applicationId;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.form.applicationId || !this.form.interviewerId || !this.form.type || !this.form.scheduledAt) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    this.loading.set(true);
    const payload = {
      applicationId: this.form.applicationId,
      interviewerId: this.form.interviewerId,
      type: this.form.type,
      scheduledAt: new Date(this.form.scheduledAt).toISOString(),
      durationMin: this.form.durationMin,
      meetingLink: this.form.meetingLink || undefined,
    };

    this.interviewService
      .schedule(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Interview scheduled');
          this.router.navigate(['/interviews']);
        },
        error: () => {
          this.toastService.error('Failed to schedule interview');
          this.loading.set(false);
        },
      });
  }
}
