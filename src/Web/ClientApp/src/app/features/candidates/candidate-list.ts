import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { CandidateService } from './candidate.service';
import { PaginatedCandidates } from './candidate.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-candidate-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, Card, Button, Badge, Spinner],
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Candidates</h1>
        <app-button routerLink="/candidates/new">New Candidate</app-button>
      </div>

      <app-card>
        <div class="mb-4">
          <input
            type="text"
            placeholder="Search by name..."
            [(ngModel)]="searchQuery"
            (ngModelChange)="loadCandidates()"
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
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Name</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Email</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Source</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Applications</th>
                  <th class="text-left py-3 px-4 font-medium text-gray-600">Created</th>
                </tr>
              </thead>
              <tbody>
                @for (candidate of data()?.items ?? []; track candidate.id) {
                  <tr
                    class="border-b border-gray-100 hover:bg-gray-50 cursor-pointer"
                    (click)="router.navigate(['/candidates', candidate.id])"
                  >
                    <td class="py-3 px-4 font-medium text-gray-900">{{ candidate.name }}</td>
                    <td class="py-3 px-4 text-gray-600">{{ candidate.email }}</td>
                    <td class="py-3 px-4">
                      <app-badge variant="info">{{ candidate.source }}</app-badge>
                    </td>
                    <td class="py-3 px-4 text-gray-600">{{ candidate.applications.length }}</td>
                    <td class="py-3 px-4 text-gray-500 text-xs">{{ candidate.created | date:'short' }}</td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5" class="py-8 text-center text-gray-500">No candidates found</td>
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
                >
                  Previous
                </app-button>
                <app-button
                  variant="secondary"
                  size="sm"
                  [disabled]="data()!.pageNumber >= data()!.totalPages"
                  (click)="goToPage(data()!.pageNumber + 1)"
                >
                  Next
                </app-button>
              </div>
            </div>
          }
        }
      </app-card>
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class CandidateList implements OnInit {
  private candidateService = inject(CandidateService);
  private toastService = inject(ToastService);
  router = inject(Router);

  data = signal<PaginatedCandidates | null>(null);
  loading = signal(false);
  searchQuery = '';
  currentPage = 1;

  ngOnInit(): void {
    this.loadCandidates();
  }

  loadCandidates(): void {
    this.loading.set(true);
    this.candidateService.getAll(this.searchQuery || undefined, this.currentPage).subscribe({
      next: (res) => {
        this.data.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load candidates');
        this.loading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadCandidates();
  }
}
