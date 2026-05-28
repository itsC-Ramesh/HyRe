import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RequisitionService } from './requisition.service';
import { RequisitionDto, PaginatedRequisitions } from './requisition.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-requisition-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, Card, Button, Badge, Spinner],
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Requisitions</h1>
        <app-button routerLink="/requisitions/new">New Requisition</app-button>
      </div>

      <app-card>
        <div class="flex gap-4 mb-4">
          <select
            [(ngModel)]="statusFilter"
            (ngModelChange)="currentPage = 1; loadRequisitions()"
            class="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">All Statuses</option>
            <option value="draft">Draft</option>
            <option value="pending_approval">Pending Approval</option>
            <option value="open">Open</option>
            <option value="on_hold">On Hold</option>
            <option value="closed">Closed</option>
          </select>
          <input
            type="text"
            placeholder="Filter by department..."
            [(ngModel)]="departmentFilter"
            (ngModelChange)="currentPage = 1; loadRequisitions()"
            class="rounded-md border border-gray-300 px-3 py-2 text-sm w-64"
          />
        </div>

        @if (loading()) {
          <div class="flex justify-center py-8">
            <app-spinner />
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-gray-200">
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Title</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Department</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Status</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Headcount</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Pipeline</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Created</th>
                </tr>
              </thead>
              <tbody>
                @for (req of data()?.items ?? []; track req.id) {
                  <tr
                    class="border-b border-gray-100 hover:bg-gray-50 cursor-pointer"
                    (click)="router.navigate(['/requisitions', req.id])"
                  >
                    <td class="py-3 px-4 font-medium text-gray-900">{{ req.title }}</td>
                    <td class="py-3 px-4 text-gray-600">{{ req.department }}</td>
                    <td class="py-3 px-4">
                      <app-badge [variant]="statusVariant(req.status)">{{ req.status }}</app-badge>
                    </td>
                    <td class="py-3 px-4 text-gray-600">{{ req.headcount }}</td>
                    <td class="py-3 px-4 text-gray-600">{{ totalPipeline(req) }}</td>
                    <td class="py-3 px-4 text-gray-500 text-xs">{{ req.created | date:'short' }}</td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="py-8 text-center text-gray-500">No requisitions found</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (data() && data()!.totalPages > 1) {
            <div class="flex items-center justify-between mt-4 pt-4 border-t border-gray-200">
              <span class="text-sm text-gray-500">
                Page {{ data()!.pageNumber }} of {{ data()!.totalPages }}
                ({{ data()!.totalCount }} total)
              </span>
              <div class="flex gap-2">
                <app-button
                  variant="secondary"
                  size="sm"
                  [disabled]="data()!.pageNumber <= 1"
                  (click)="goToPage(data()!.pageNumber - 1)"
                >Previous</app-button>
                <app-button
                  variant="secondary"
                  size="sm"
                  [disabled]="data()!.pageNumber >= data()!.totalPages"
                  (click)="goToPage(data()!.pageNumber + 1)"
                >Next</app-button>
              </div>
            </div>
          }
        }
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class RequisitionList implements OnInit {
  private requisitionService = inject(RequisitionService);
  private toastService = inject(ToastService);
  router = inject(Router);

  data = signal<PaginatedRequisitions | null>(null);
  loading = signal(false);
  statusFilter = '';
  departmentFilter = '';
  currentPage = 1;

  ngOnInit(): void {
    this.loadRequisitions();
  }

  loadRequisitions(): void {
    this.loading.set(true);
    this.requisitionService
      .getAll(this.statusFilter || undefined, this.departmentFilter || undefined, this.currentPage)
      .subscribe({
        next: (res) => {
          this.data.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load requisitions');
          this.loading.set(false);
        },
      });
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadRequisitions();
  }

  statusVariant(status: string): 'info' | 'success' | 'warning' | 'danger' {
    const map: Record<string, 'info' | 'success' | 'warning' | 'danger'> = {
      draft: 'info',
      pending_approval: 'warning',
      open: 'success',
      on_hold: 'warning',
      closed: 'danger',
    };
    return map[status] ?? 'info';
  }

  totalPipeline(req: RequisitionDto): number {
    return Object.values(req.applicationCountByStage).reduce((a, b) => a + b, 0);
  }
}
