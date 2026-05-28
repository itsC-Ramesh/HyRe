import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CdkDragDrop, CdkDropList, CdkDrag, transferArrayItem } from '@angular/cdk/drag-drop';
import { PipelineService } from './pipeline.service';
import { PipelineDto, PipelineApplicationCard, PipelineStageGroup } from './pipeline.models';
import { PipelineCard } from './pipeline-card';
import { Button } from '../../shared/ui/button/button';
import { Badge } from '../../shared/ui/badge/badge';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

const STAGE_ORDER = ['Applied', 'Screened', 'Interview', 'Offer', 'Hired'];

@Component({
  selector: 'app-pipeline-board',
  standalone: true,
  imports: [RouterLink, CdkDropList, CdkDrag, PipelineCard, Button, Badge, Spinner],
  template: `
    @if (loading()) {
      <div class="flex justify-center py-12">
        <app-spinner />
      </div>
    } @else if (pipeline()) {
      <div class="mb-6">
        <div class="flex items-center justify-between">
          <div>
            <a [routerLink]="['/requisitions', pipeline()!.requisitionId]"
               class="text-sm text-primary-600 hover:text-primary-700">
              &larr; Back to Requisition
            </a>
            <h1 class="text-2xl font-bold text-gray-900 mt-1">{{ pipeline()!.requisitionTitle }}</h1>
          </div>
          <div class="flex gap-2">
            @if (selectedIds().length > 0) {
              <app-button size="sm" (click)="bulkAdvance()">
                Move {{ selectedIds().length }} to Next Stage
              </app-button>
            }
            <app-button variant="secondary" size="sm" (click)="refresh()">Refresh</app-button>
          </div>
        </div>
      </div>

      <div class="flex gap-4 overflow-x-auto pb-4" cdkDropListGroup>
        @for (stage of stages(); track stage.stage) {
          <div class="flex-shrink-0 w-72">
            <div class="bg-gray-50 rounded-lg p-3">
              <div class="flex items-center justify-between mb-3">
                <h3 class="text-sm font-semibold text-gray-700">{{ stage.stage }}</h3>
                <app-badge variant="info">{{ stage.applications.length }}</app-badge>
              </div>

              <div
                cdkDropList
                [cdkDropListData]="stage.applications"
                [cdkDropListConnectedTo]="dropListIds()"
                [id]="stage.stage"
                [cdkDropListSortPredicate]="sortPredicate"
                (cdkDropListDropped)="onDrop($event)"
                class="min-h-[100px] space-y-0"
              >
                @for (app of stage.applications; track app.applicationId) {
                  <app-pipeline-card [application]="app" />
                } @empty {
                  <div class="text-center py-6 text-sm text-gray-400">
                    No candidates
                  </div>
                }
              </div>
            </div>
          </div>
        }
      </div>

      @if (rejectedApplications().length > 0) {
        <div class="mt-6">
          <h3 class="text-sm font-semibold text-gray-700 mb-3">
            Rejected
            <app-badge variant="danger">{{ rejectedApplications().length }}</app-badge>
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            @for (app of rejectedApplications(); track app.applicationId) {
              <div class="bg-gray-50 rounded-lg border border-gray-200 p-3">
                <a
                  [routerLink]="['/candidates', app.candidateId]"
                  class="text-sm font-medium text-gray-900 hover:text-primary-600"
                >
                  {{ app.candidateName }}
                </a>
                <p class="text-xs text-gray-500 mt-0.5">{{ app.candidateEmail }}</p>
              </div>
            }
          </div>
        </div>
      }
    }
  `,
  styles: `:host { display: block; }`,
})
export class PipelineBoard implements OnInit {
  private pipelineService = inject(PipelineService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pipeline = signal<PipelineDto | null>(null);
  loading = signal(false);
  stages = signal<PipelineStageGroup[]>([]);
  rejectedApplications = signal<PipelineApplicationCard[]>([]);
  dropListIds = signal<string[]>([]);
  selectedIds = signal<string[]>([]);

  sortPredicate = () => false;

  ngOnInit(): void {
    const requisitionId = this.route.snapshot.params['requisitionId'];
    this.loadPipeline(requisitionId);
  }

  private loadPipeline(requisitionId: string): void {
    this.loading.set(true);
    this.pipelineService.getByRequisition(requisitionId).subscribe({
      next: (data) => {
        this.pipeline.set(data);
        this.organizeStages(data);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load pipeline');
        this.router.navigate(['/requisitions']);
      },
    });
  }

  private organizeStages(data: PipelineDto): void {
    const stageMap = new Map<string, PipelineApplicationCard[]>();
    for (const s of data.stages) {
      stageMap.set(s.stage, s.applications);
    }

    const ordered: PipelineStageGroup[] = [];
    for (const stageName of STAGE_ORDER) {
      ordered.push({
        stage: stageName,
        applications: stageMap.get(stageName) ?? [],
      });
    }

    this.stages.set(ordered);
    this.rejectedApplications.set(stageMap.get('Rejected') ?? []);
    this.dropListIds.set(STAGE_ORDER);
  }

  refresh(): void {
    if (this.pipeline()) {
      this.loadPipeline(this.pipeline()!.requisitionId);
    }
  }

  onDrop(event: CdkDragDrop<PipelineApplicationCard[]>): void {
    if (event.previousContainer === event.container) return;

    const app = event.item.data as PipelineApplicationCard;
    const newStage = event.container.id;

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex,
    );

    this.pipelineService.advance(app.applicationId, newStage).subscribe({
      next: () => {
        this.toastService.success('Moved to ' + newStage);
        this.refresh();
      },
      error: () => {
        this.toastService.error('Failed to advance');
        this.refresh();
      },
    });
  }

  bulkAdvance(): void {
    const ids = this.selectedIds();
    if (ids.length === 0) return;

    const allApps = this.stages().flatMap((s) =>
      s.applications.map((a) => ({ ...a, currentStage: s.stage }))
    );
    const selected = allApps.filter((a) => ids.includes(a.applicationId));
    const nextStageMap: Record<string, string> = {};
    for (let i = 0; i < STAGE_ORDER.length - 1; i++) {
      nextStageMap[STAGE_ORDER[i]] = STAGE_ORDER[i + 1];
    }

    const groups = new Map<string, string[]>();
    for (const app of selected) {
      const next = nextStageMap[app.currentStage];
      if (next) {
        const arr = groups.get(next) ?? [];
        arr.push(app.applicationId);
        groups.set(next, arr);
      }
    }

    for (const [stage, appIds] of groups) {
      this.pipelineService.bulkAdvance(appIds, stage).subscribe({
        next: () => {
          this.toastService.success('Moved ' + appIds.length + ' to ' + stage);
          this.selectedIds.set([]);
          this.refresh();
        },
        error: () => this.toastService.error('Bulk advance failed'),
      });
    }
  }
}
