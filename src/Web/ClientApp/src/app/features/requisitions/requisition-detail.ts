import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { RequisitionService } from './requisition.service';
import { RequisitionDto } from './requisition.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-requisition-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, Card, Button, Badge, Spinner],
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
  `,
  styles: `:host { display: block; }`,
})
export class RequisitionDetail implements OnInit {
  private requisitionService = inject(RequisitionService);
  private toastService = inject(ToastService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  requisition = signal<RequisitionDto | null>(null);
  loading = signal(false);
  pipelineStages = signal<{ name: string; count: number }[]>([]);
  statusVariant = signal<'info' | 'success' | 'warning' | 'danger'>('info');

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    this.loadRequisition(id);
  }

  private loadRequisition(id: string): void {
    this.loading.set(true);
    this.requisitionService.getById(id).subscribe({
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
    this.requisitionService.submit(this.requisition()!.id).subscribe({
      next: () => {
        this.toastService.success('Submitted for approval');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to submit'),
    });
  }

  approve(): void {
    this.requisitionService.approve(this.requisition()!.id).subscribe({
      next: () => {
        this.toastService.success('Requisition approved');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to approve'),
    });
  }

  reject(): void {
    const reason = prompt('Enter rejection reason:');
    if (!reason) return;
    this.requisitionService.reject(this.requisition()!.id, reason).subscribe({
      next: () => {
        this.toastService.success('Requisition rejected');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to reject'),
    });
  }

  hold(): void {
    this.requisitionService.hold(this.requisition()!.id).subscribe({
      next: () => {
        this.toastService.success('Requisition put on hold');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to hold'),
    });
  }

  close(): void {
    if (!confirm('Are you sure you want to close this requisition?')) return;
    this.requisitionService.close(this.requisition()!.id).subscribe({
      next: () => {
        this.toastService.success('Requisition closed');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to close'),
    });
  }

  reopen(): void {
    this.requisitionService.submit(this.requisition()!.id).subscribe({
      next: () => {
        this.toastService.success('Requisition reopened');
        this.loadRequisition(this.requisition()!.id);
      },
      error: () => this.toastService.error('Failed to reopen'),
    });
  }

  clone(): void {
    this.requisitionService.clone(this.requisition()!.id).subscribe({
      next: (newId) => {
        this.toastService.success('Requisition cloned');
        this.router.navigate(['/requisitions', newId]);
      },
      error: () => this.toastService.error('Failed to clone'),
    });
  }
}
