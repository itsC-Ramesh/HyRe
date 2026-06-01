import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { RequisitionService } from './requisition.service';
import { RequisitionDto } from './requisition.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-requisition-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, Card, Button, Badge, Spinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <div class="flex justify-center py-12">
        <app-spinner />
      </div>
    } @else if (requisition()) {
      <div class="max-w-4xl mx-auto">
        <!-- Header -->
        <div class="flex items-center justify-between mb-6">
          <div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl font-bold text-gray-900">{{ requisition()!.title }}</h1>
              <app-badge [variant]="statusVariant()">{{ requisition()!.status }}</app-badge>
            </div>
            <p class="text-gray-500 mt-1">{{ requisition()!.department }}</p>
          </div>
          <div class="flex gap-2">
            @if (requisition()!.status === 'draft') {
              <app-button size="sm" (click)="submit()">Submit for Approval</app-button>
              <app-button variant="secondary" size="sm" routerLink="/requisitions/{{ requisition()!.id }}/edit">
                Edit
              </app-button>
            }
            @if (requisition()!.status === 'pending_approval') {
              <app-button size="sm" (click)="approve()">Approve</app-button>
              <app-button variant="danger" size="sm" (click)="reject()">Reject</app-button>
            }
            @if (requisition()!.status === 'open') {
              <app-button size="sm" routerLink="/pipeline/{{ requisition()!.id }}">View Pipeline</app-button>
              <app-button variant="secondary" size="sm" (click)="hold()">Hold</app-button>
              <app-button variant="danger" size="sm" (click)="close()">Close</app-button>
            }
            @if (requisition()!.status === 'on_hold') {
              <app-button size="sm" (click)="reopen()">Reopen</app-button>
              <app-button variant="danger" size="sm" (click)="close()">Close</app-button>
            }
            @if (requisition()!.status === 'closed') {
              <app-button size="sm" (click)="clone()">Clone</app-button>
            }
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Main content -->
          <div class="lg:col-span-2 space-y-6">
            <app-card title="Job Description">
              <p class="text-gray-700 whitespace-pre-wrap">{{ requisition()!.jdText }}</p>
            </app-card>

            <app-card title="Salary Band">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <p class="text-sm text-gray-500">Minimum</p>
                  <p class="text-lg font-medium text-gray-900">
                    {{ requisition()!.salaryMin ? ('₹' + requisition()!.salaryMin!.toLocaleString()) : '—' }}
                  </p>
                </div>
                <div>
                  <p class="text-sm text-gray-500">Maximum</p>
                  <p class="text-lg font-medium text-gray-900">
                    {{ requisition()!.salaryMax ? ('₹' + requisition()!.salaryMax!.toLocaleString()) : '—' }}
                  </p>
                </div>
              </div>
            </app-card>
          </div>

          <!-- Sidebar -->
          <div class="space-y-6">
            <app-card title="Details">
              <dl class="space-y-3">
                <div>
                  <dt class="text-sm text-gray-500">Headcount</dt>
                  <dd class="text-sm font-medium text-gray-900">{{ requisition()!.headcount }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500">Created</dt>
                  <dd class="text-sm font-medium text-gray-900">{{ requisition()!.created | date:'medium' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500">Last Modified</dt>
                  <dd class="text-sm font-medium text-gray-900">{{ requisition()!.lastModified | date:'medium' }}</dd>
                </div>
              </dl>
            </app-card>

            <app-card title="Pipeline Summary">
              <div class="space-y-2">
                @for (stage of pipelineStages(); track stage.name) {
                  <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-600">{{ stage.name }}</span>
                    <span class="text-sm font-medium text-gray-900">{{ stage.count }}</span>
                  </div>
                }
                @if (pipelineStages().length === 0) {
                  <p class="text-sm text-gray-500">No applications yet</p>
                }
              </div>
              @if (requisition()!.status === 'open') {
                <div class="mt-4 pt-4 border-t border-gray-200">
                  <app-button size="sm" routerLink="/pipeline/{{ requisition()!.id }}">
                    View Pipeline
                  </app-button>
                </div>
              }
            </app-card>
          </div>
        </div>
      </div>
    }

    @if (rejectDialogOpen()) {
      <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center" (click)="rejectDialogOpen.set(false)">
        <div class="bg-white rounded-lg shadow-xl w-full max-w-md mx-4 p-6" (click)="$event.stopPropagation()">
          <h3 class="text-lg font-medium text-gray-900 mb-4">Reject Requisition</h3>
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Rejection Reason *</label>
            <textarea
              rows="4"
              [ngModel]="rejectReason()"
              (ngModelChange)="rejectReason.set($event)"
              class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
              placeholder="Enter reason for rejection..."
            ></textarea>
          </div>
          <div class="flex justify-end gap-3">
            <app-button variant="secondary" size="sm" (click)="rejectDialogOpen.set(false)">Cancel</app-button>
            <app-button variant="danger" size="sm" [disabled]="!rejectReason()" (click)="confirmReject()">Reject</app-button>
          </div>
        </div>
      </div>
    }

    @if (closeDialogOpen()) {
      <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center" (click)="closeDialogOpen.set(false)">
        <div class="bg-white rounded-lg shadow-xl w-full max-w-md mx-4 p-6" (click)="$event.stopPropagation()">
          <h3 class="text-lg font-medium text-gray-900 mb-2">Close Requisition</h3>
          <p class="text-sm text-gray-600 mb-6">Are you sure you want to close this requisition? This action can be undone by reopening.</p>
          <div class="flex justify-end gap-3">
            <app-button variant="secondary" size="sm" (click)="closeDialogOpen.set(false)">Cancel</app-button>
            <app-button variant="danger" size="sm" (click)="confirmClose()">Close</app-button>
          </div>
        </div>
      </div>
    }
  `,
  styles: `:host { display: block; }`,
})
export class RequisitionDetail implements OnInit, OnDestroy {
  private requisitionService = inject(RequisitionService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  requisition = signal<RequisitionDto | null>(null);
  loading = signal(false);
  pipelineStages = signal<{ name: string; count: number }[]>([]);
  statusVariant = signal<'info' | 'success' | 'warning' | 'danger'>('info');
  rejectDialogOpen = signal(false);
  rejectReason = signal('');
  closeDialogOpen = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    this.loadRequisition(id);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadRequisition(id: string): void {
    this.loading.set(true);
    this.requisitionService.getById(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (req) => {
        this.requisition.set(req);
        this.statusVariant.set(this.getStatusVariant(req.status));
        this.pipelineStages.set(
          Object.entries(req.applicationCountByStage).map(([name, count]) => ({ name, count }))
        );
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load requisition');
        this.router.navigate(['/requisitions']);
      },
    });
  }

  private getStatusVariant(status: string): 'info' | 'success' | 'warning' | 'danger' {
    const map: Record<string, 'info' | 'success' | 'warning' | 'danger'> = {
      draft: 'info',
      pending_approval: 'warning',
      open: 'success',
      on_hold: 'warning',
      closed: 'danger',
    };
    return map[status] ?? 'info';
  }

  submit(): void {
    this.requisitionService.submit(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Submitted for approval');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to submit'),
    });
  }

  approve(): void {
    this.requisitionService.approve(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Requisition approved');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to approve'),
    });
  }

  reject(): void {
    this.rejectReason.set('');
    this.rejectDialogOpen.set(true);
  }

  confirmReject(): void {
    const reason = this.rejectReason();
    if (!reason) return;
    this.rejectDialogOpen.set(false);
    this.requisitionService.reject(this.requisition()!.id, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Requisition rejected');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to reject'),
    });
  }

  hold(): void {
    this.requisitionService.hold(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Requisition put on hold');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to hold'),
    });
  }

  close(): void {
    this.closeDialogOpen.set(true);
  }

  confirmClose(): void {
    this.closeDialogOpen.set(false);
    this.requisitionService.close(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Requisition closed');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to close'),
    });
  }

  reopen(): void {
    this.requisitionService.submit(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toastService.success('Requisition reopened');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to reopen'),
    });
  }

  clone(): void {
    this.requisitionService.clone(this.requisition()!.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (newId) => {
        this.toastService.success('Requisition cloned');
        this.router.navigate(['/requisitions', newId]);
      },
      error: () => this.toastService.error('Failed to clone'),
    });
  }
}
