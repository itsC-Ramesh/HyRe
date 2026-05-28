import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CandidateService } from './candidate.service';
import { CandidateSource } from './candidate.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-candidate-form',
  standalone: true,
  imports: [FormsModule, Card, Button],
  template: `
    <div class="max-w-3xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">
        {{ isEdit() ? 'Edit Candidate' : 'New Candidate' }}
      </h1>

      <app-card>
        <form (submit)="onSubmit($event)" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
            <input
              type="text"
              [(ngModel)]="form.name"
              name="name"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="Full name"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Email *</label>
            <input
              type="email"
              [(ngModel)]="form.email"
              name="email"
              required
              [disabled]="isEdit()"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="email@example.com"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Phone</label>
            <input
              type="tel"
              [(ngModel)]="form.phone"
              name="phone"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="+91 98765 43210"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Source *</label>
            <select
              [(ngModel)]="form.source"
              name="source"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            >
              <option value="">Select source...</option>
              <option value="direct">Direct</option>
              <option value="linkedin">LinkedIn</option>
              <option value="job_board">Job Board</option>
              <option value="referral">Referral</option>
              <option value="agency">Agency</option>
              <option value="headhunted">Headhunted</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Source Detail</label>
            <input
              type="text"
              [(ngModel)]="form.sourceDetail"
              name="sourceDetail"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="e.g. Referrer name, agency name"
            />
          </div>

          <div class="flex gap-3 pt-4">
            <app-button type="submit" [loading]="loading()">
              {{ isEdit() ? 'Update' : 'Create' }}
            </app-button>
            <app-button variant="secondary" (click)="router.navigate(['/candidates'])">
              Cancel
            </app-button>
          </div>
        </form>
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class CandidateForm implements OnInit {
  private candidateService = inject(CandidateService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  router = inject(Router);

  isEdit = signal(false);
  loading = signal(false);
  candidateId = '';

  form = {
    name: '',
    email: '',
    phone: '',
    source: '' as CandidateSource | '',
    sourceDetail: '',
  };

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.params['id'];
    if (this.candidateId) {
      this.isEdit.set(true);
      this.candidateService.getById(this.candidateId).subscribe({
        next: (c) => {
          this.form = {
            name: c.name,
            email: c.email,
            phone: c.phone ?? '',
            source: c.source,
            sourceDetail: c.sourceDetail ?? '',
          };
        },
        error: () => this.toastService.error('Failed to load candidate'),
      });
    }
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.form.name || !this.form.email || !this.form.source) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    this.loading.set(true);
    const payload = {
      name: this.form.name,
      email: this.form.email,
      phone: this.form.phone || undefined,
      source: this.form.source,
      sourceDetail: this.form.sourceDetail || undefined,
    };

    if (this.isEdit()) {
      this.candidateService.update(this.candidateId, payload).subscribe({
        next: () => {
          this.toastService.success('Candidate updated');
          this.router.navigate(['/candidates']);
        },
        error: () => {
          this.toastService.error('Failed to save candidate');
          this.loading.set(false);
        },
      });
    } else {
      this.candidateService.create(payload).subscribe({
        next: () => {
          this.toastService.success('Candidate created');
          this.router.navigate(['/candidates']);
        },
        error: () => {
          this.toastService.error('Failed to save candidate');
          this.loading.set(false);
        },
      });
    }
  }
}
