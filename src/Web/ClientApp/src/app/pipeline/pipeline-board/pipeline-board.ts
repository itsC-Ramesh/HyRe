import { Component, OnInit } from '@angular/core';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../web-api-client';
import { Inject } from '@angular/core';

interface Candidate {
  id: string;
  name: string;
  source: string;
  appliedDate: Date;
}

interface PipelineStage {
  id: string;
  name: string;
  stage: number;
  candidates: Candidate[];
}

@Component({
  selector: 'app-pipeline-board',
  templateUrl: './pipeline-board.html',
  styleUrls: ['./pipeline-board.css']
})
export class PipelineBoardComponent implements OnInit {
  requisitionId: string = '';
  stages: PipelineStage[] = [];
  isLoading = true;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.requisitionId = params.get('id') || '';
      if (this.requisitionId) {
        this.loadPipeline();
      }
    });
  }

  loadPipeline(): void {
    this.isLoading = true;
    this.http.get<any>(`${this.baseUrl}/api/v1/pipeline/requisition/${this.requisitionId}`)
      .subscribe({
        next: (res) => {
          // Map to local UI models. Depending on backend response.
          // Using mock data mapping if backend returns empty or is not connected.
          if (res && res.data) {
             this.stages = res.data;
          } else {
             this.loadMockData();
          }
          this.isLoading = false;
        },
        error: () => {
          this.loadMockData();
          this.isLoading = false;
        }
      });
  }

  loadMockData(): void {
    this.stages = [
      { id: '1', name: 'Applied', stage: 0, candidates: [{ id: 'c1', name: 'Alice Smith', source: 'LinkedIn', appliedDate: new Date() }] },
      { id: '2', name: 'Screening', stage: 1, candidates: [{ id: 'c2', name: 'Bob Jones', source: 'Referral', appliedDate: new Date() }] },
      { id: '3', name: 'Interviewing', stage: 2, candidates: [] },
      { id: '4', name: 'Offered', stage: 3, candidates: [] },
      { id: '5', name: 'Hired', stage: 4, candidates: [] }
    ];
  }

  drop(event: CdkDragDrop<Candidate[]>, stage: PipelineStage) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      const candidate = event.container.data[event.currentIndex];
      this.advanceCandidate(candidate.id, stage.stage);
    }
  }

  advanceCandidate(applicationId: string, newStage: number): void {
    // Optimistic UI update already happened above.
    // Sync with backend.
    this.http.post(`${this.baseUrl}/api/v1/pipeline/applications/${applicationId}/advance`, { newStage })
      .subscribe({
        next: () => console.log('Advanced successfully'),
        error: (err) => console.error('Failed to advance', err)
      });
  }

  get connectedDropLists(): string[] {
    return this.stages.map(s => 'stage-' + s.id);
  }
}
