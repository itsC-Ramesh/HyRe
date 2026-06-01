import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { RequisitionService } from './requisition.service';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-requisition-form',
  standalone: true,
  imports: [FormsModule, Card, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-3xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">
        {{ isEdit() ? 'Edit Requisition' : 'New Requisition' }}
      </h1>

      <app-card>
        <form (submit)="onSubmit($event)" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Title *</label>
            <input
              type="text"
              [(ngModel)]="form.title"
              name="title"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="e.g. Senior Software Engineer"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Department *</label>
            <input
              type="text"
              [(ngModel)]="form.department"
              name="department"
              required
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="e.g. Engineering"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Job Description *</label>
            <textarea
              [(ngModel)]="form.jdText"
              name="jdText"
              rows="6"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              placeholder="Describe the role, responsibilities, and requirements..."
            ></textarea>
          </div>

          <div class="grid grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Salary Min</label>
              <input
                type="number"
                [(ngModel)]="form.salaryMin"
                name="salaryMin"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                placeholder="e.g. 800000"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Salary Max</label>
              <input
                type="number"
                [(ngModel)]="form.salaryMax"
                name="salaryMax"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                placeholder="e.g. 1500000"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Headcount *</label>
              <input
                type="number"
                [(ngModel)]="form.headcount"
                name="headcount"
                required
                min="1"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                placeholder="1"
              />
            </div>
          </div>

          <div class="flex gap-3 pt-4">
            <app-button type="submit" [loading]="loading()">
              {{ isEdit() ? 'Update' : 'Create' }}
            </app-button>
            <app-button variant="secondary" (click)="cancel()">
              Cancel
            </app-button>
          </div>
        </form>
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class RequisitionForm implements OnInit, OnDestroy {
  private requisitionService = inject(RequisitionService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  isEdit = signal(false);
  loading = signal(false);
  requisitionId = '';

  form = {
    title: '',
    department: '',
    jdText: '',
    salaryMin: null as number | null,
    salaryMax: null as number | null,
    headcount: 1,
  };

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cancel(): void {
    this.router.navigate(['/requisitions']);
  }

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.params['id'];
    if (this.requisitionId) {
      this.isEdit.set(true);
      this.requisitionService.getById(this.requisitionId).pipe(takeUntil(this.destroy$)).subscribe({
        next: (req) => {
          this.form = {
            title: req.title,
            department: req.department,
            jdText: req.jdText,
            salaryMin: req.salaryMin,
            salaryMax: req.salaryMax,
            headcount: req.headcount,
          };
        },
        error: () => this.toastService.error('Failed to load requisition'),
      });
    }
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.form.title || !this.form.department || !this.form.jdText) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    this.loading.set(true);
    const payload = {
      title: this.form.title,
      department: this.form.department,
      jdText: this.form.jdText,
      salaryMin: this.form.salaryMin ?? undefined,
      salaryMax: this.form.salaryMax ?? undefined,
      headcount: this.form.headcount,
    };

    if (this.isEdit()) {
      this.requisitionService.update(this.requisitionId, payload).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => {
          this.toastService.success('Requisition updated');
          this.router.navigate(['/requisitions']);
        },
        error: () => {
          this.toastService.error('Failed to save requisition');
          this.loading.set(false);
        },
      });
    } else {
      this.requisitionService.create(payload).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => {
          this.toastService.success('Requisition created');
          this.router.navigate(['/requisitions']);
        },
        error: () => {
          this.toastService.error('Failed to save requisition');
          this.loading.set(false);
        },
      });
    }
  }
}
