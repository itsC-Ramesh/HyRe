import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CandidateService } from './candidate.service';
import { CandidateDto } from './candidate.models';
import { RequisitionService } from '../requisitions/requisition.service';
import { RequisitionDto } from '../requisitions/requisition.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-candidate-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, Card, Button, Badge, Spinner],
  template: `
    @if (loading()) {
      <div class="flex justify-center py-12">
        <app-spinner />
      </div>
    } @else if (candidate()) {
      <div class="max-w-4xl mx-auto">
        <!-- Header -->
        <div class="flex items-center justify-between mb-6">
          <div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl font-bold text-gray-900">{{ candidate()!.name }}</h1>
              <app-badge variant="info">{{ candidate()!.source }}</app-badge>
            </div>
            <p class="text-gray-500 mt-1">{{ candidate()!.email }}</p>
          </div>
          <app-button variant="secondary" routerLink="/candidates/{{ candidate()!.id }}/edit">
            Edit
          </app-button>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Main content -->
          <div class="lg:col-span-2 space-y-6">
            <!-- Applications -->
            <app-card title="Applications">
              @if (candidate()!.applications.length > 0) {
                <div class="overflow-x-auto">
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b border-gray-200">
                        <th class="text-left py-2 px-3 font-medium text-gray-600">Requisition</th>
                        <th class="text-left py-2 px-3 font-medium text-gray-600">Stage</th>
                        <th class="text-left py-2 px-3 font-medium text-gray-600">Applied</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (app of candidate()!.applications; track app.applicationId) {
                        <tr
                          class="border-b border-gray-100 hover:bg-gray-50 cursor-pointer"
                          (click)="router.navigate(['/pipeline', app.requisitionId])"
                        >
                          <td class="py-2 px-3 text-gray-900">{{ app.requisitionTitle }}</td>
                          <td class="py-2 px-3">
                            <app-badge [variant]="stageVariant(app.stage)">{{ app.stage }}</app-badge>
                          </td>
                          <td class="py-2 px-3 text-gray-500 text-xs">{{ app.created | date:'short' }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              } @else {
                <p class="text-gray-500 text-sm">No applications yet</p>
              }

              <!-- Apply to requisition -->
              <div class="mt-4 pt-4 border-t border-gray-200">
                <div class="flex items-center gap-3">
                  <select
                    [(ngModel)]="selectedRequisitionId"
                    class="rounded-md border border-gray-300 px-3 py-2 text-sm flex-1"
                  >
                    <option value="">Select open requisition...</option>
                    @for (req of openRequisitions(); track req.id) {
                      <option [value]="req.id">{{ req.title }} ({{ req.department }})</option>
                    }
                  </select>
                  <app-button
                    size="sm"
                    [disabled]="!selectedRequisitionId"
                    (click)="applyToRequisition()"
                  >
                    Apply
                  </app-button>
                </div>
              </div>
            </app-card>
          </div>

          <!-- Sidebar -->
          <div class="space-y-6">
            <app-card title="Contact">
              <dl class="space-y-3">
                <div>
                  <dt class="text-sm text-gray-500">Email</dt>
                  <dd class="text-sm font-medium text-gray-900">{{ candidate()!.email }}</dd>
                </div>
                @if (candidate()!.phone) {
                  <div>
                    <dt class="text-sm text-gray-500">Phone</dt>
                    <dd class="text-sm font-medium text-gray-900">{{ candidate()!.phone }}</dd>
                  </div>
                }
                <div>
                  <dt class="text-sm text-gray-500">Source</dt>
                  <dd class="text-sm font-medium text-gray-900">
                    {{ candidate()!.source }}
                    @if (candidate()!.sourceDetail) {
                      <span class="text-gray-500">({{ candidate()!.sourceDetail }})</span>
                    }
                  </dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500">Added</dt>
                  <dd class="text-sm font-medium text-gray-900">{{ candidate()!.created | date:'medium' }}</dd>
                </div>
              </dl>
            </app-card>
          </div>
        </div>
      </div>
    }
  `,
  styles: `:host { display: block; }`,
})
export class CandidateDetail implements OnInit {
  private candidateService = inject(CandidateService);
  private requisitionService = inject(RequisitionService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  router = inject(Router);

  candidate = signal<CandidateDto | null>(null);
  loading = signal(false);
  openRequisitions = signal<RequisitionDto[]>([]);
  selectedRequisitionId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    this.loadCandidate(id);
    this.loadOpenRequisitions();
  }

  private loadCandidate(id: string): void {
    this.loading.set(true);
    this.candidateService.getById(id).subscribe({
      next: (c) => {
        this.candidate.set(c);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load candidate');
        this.router.navigate(['/candidates']);
      },
    });
  }

  private loadOpenRequisitions(): void {
    this.requisitionService.getAll('open', undefined, 1, 100).subscribe({
      next: (res) => this.openRequisitions.set(res.items),
      error: () => {},
    });
  }

  applyToRequisition(): void {
    if (!this.selectedRequisitionId) return;
    this.candidateService.applyToRequisition(this.candidate()!.id, this.selectedRequisitionId).subscribe({
      next: () => {
        this.toastService.success('Applied to requisition');
        this.selectedRequisitionId = '';
        this.loadCandidate(this.candidate()!.id);
      },
      error: () => this.toastService.error('Failed to apply'),
    });
  }

  stageVariant(stage: string): 'info' | 'success' | 'warning' | 'danger' {
    const map: Record<string, 'info' | 'success' | 'warning' | 'danger'> = {
      applied: 'info',
      screened: 'info',
      interview: 'warning',
      offer: 'success',
      hired: 'success',
      rejected: 'danger',
    };
    return map[stage] ?? 'info';
  }
}
