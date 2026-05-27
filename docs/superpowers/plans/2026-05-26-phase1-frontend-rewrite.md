# Phase 1 Frontend Rewrite — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite all Phase 1 Angular components with proper service layer, correct API URLs (`/api/...`), shared models, and real data integration — eliminating all mock data fallbacks.

**Architecture:** Feature-slice approach: shared foundation (models, services, routing) first, then one feature at a time (requisitions → pipeline → candidates → interviews → scorecards). Each component uses `HttpClient` directly via injected services. No NSwag client. No silent mock fallbacks.

**Tech Stack:** Angular 21, TypeScript, HttpClient, ReactiveFormsModule, @angular/cdk (drag-drop), Pico CSS, Lucide icons

---

## File Structure

| Action | File | Purpose |
|--------|------|---------|
| Create | `src/app/models/index.ts` | Barrel export for all shared models |
| Create | `src/app/models/requisition.model.ts` | Requisition types |
| Create | `src/app/models/candidate.model.ts` | Candidate types |
| Create | `src/app/models/pipeline.model.ts` | Pipeline types |
| Create | `src/app/models/interview.model.ts` | Interview types |
| Create | `src/app/models/scorecard.model.ts` | Scorecard types |
| Create | `src/app/models/note.model.ts` | Note types |
| Create | `src/app/models/tag.model.ts` | Tag types |
| Create | `src/app/models/event-log.model.ts` | Event log / communication types |
| Create | `src/app/models/common.model.ts` | PaginatedResult, ApiError |
| Create | `src/app/services/api.service.ts` | Base HTTP service with error handling |
| Create | `src/app/services/requisition.service.ts` | Requisition API calls |
| Create | `src/app/services/candidate.service.ts` | Candidate API calls |
| Create | `src/app/services/pipeline.service.ts` | Pipeline API calls |
| Create | `src/app/services/interview.service.ts` | Interview API calls |
| Create | `src/app/services/scorecard.service.ts` | Scorecard API calls |
| Create | `src/app/services/note.service.ts` | Note API calls |
| Create | `src/app/services/tag.service.ts` | Tag API calls |
| Create | `src/app/services/communication.service.ts` | Communication feed API |
| Modify | `src/app/app.module.ts` | Register new components and routes |
| Modify | `src/app/nav-menu/nav-menu.component.html` | Add missing nav links |
| Rewrite | `src/app/requisitions/requisitions-list/*` | Real API, filter, pagination, workflow |
| Rewrite | `src/app/requisitions/requisition-form/*` | Real API, workflow actions |
| Rewrite | `src/app/pipeline/pipeline-board/*` | Real API, links, bulk ops, reject |
| Rewrite | `src/app/candidates/candidate-detail/*` | Real API, all sections |
| Create | `src/app/candidates/candidate-list/*` | New candidate list component |
| Rewrite | `src/app/candidates/schedule-interview/*` | Real API, dynamic interviewers |
| Rewrite | `src/app/interviewer/pending-interviews/*` | Real API, correct navigation |
| Rewrite | `src/app/interviewer/scorecard-form/*` | Real API, view existing, draft save |
| Create | `src/app/interviewer/scorecard-view/*` | New read-only scorecard display |
| Create | `src/app/interviewer/availability/*` | New availability management |

---

### Task 1: Create shared models

- [ ] **Step 1: Create common model types**

Create `src/app/models/common.model.ts`:

```typescript
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  limit: number;
  totalPages: number;
}

export interface ApiErrorResponse {
  code: string;
  message: string;
  errors?: string[];
}

export interface ApiResponse<T> {
  succeeded: boolean;
  data?: T;
  error?: ApiErrorResponse;
}
```

- [ ] **Step 2: Create requisition model**

Create `src/app/models/requisition.model.ts`:

```typescript
export interface Requisition {
  id: string;
  title: string;
  department: string;
  ownerId: string;
  jdText: string;
  salaryMin?: number;
  salaryMax?: number;
  headcount: number;
  status: RequisitionStatus;
  applicationCountByStage?: Record<string, number>;
  createdAt: string;
  updatedAt: string;
}

export enum RequisitionStatus {
  Draft = 0,
  PendingApproval = 1,
  Open = 2,
  OnHold = 3,
  Closed = 4,
}

export interface RequisitionFilters {
  status?: RequisitionStatus;
  department?: string;
  page?: number;
  limit?: number;
}
```

- [ ] **Step 3: Create candidate model**

Create `src/app/models/candidate.model.ts`:

```typescript
export interface Candidate {
  id: string;
  name: string;
  email: string;
  phone?: string;
  source: CandidateSource;
  sourceDetail?: string;
  resumeDocId?: string;
  applications: CandidateApplication[];
  tags: CandidateTagAssignment[];
  createdAt: string;
}

export enum CandidateSource {
  Direct = 0,
  LinkedIn = 1,
  JobBoard = 2,
  Referral = 3,
  Agency = 4,
  Headhunted = 5,
}

export interface CandidateApplication {
  id: string;
  requisitionId: string;
  requisitionTitle: string;
  stage: number;
  createdAt: string;
}

export interface CandidateTagAssignment {
  tagId: string;
  tagName: string;
  tagColor: string;
  assignedAt: string;
}
```

- [ ] **Step 4: Create pipeline model**

Create `src/app/models/pipeline.model.ts`:

```typescript
export interface PipelineStageGroup {
  stage: number;
  stageName: string;
  applications: PipelineApplicationCard[];
}

export interface PipelineApplicationCard {
  applicationId: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  source: string;
  daysInStage: number;
  createdAt: string;
}
```

- [ ] **Step 5: Create interview model**

Create `src/app/models/interview.model.ts`:

```typescript
export interface Interview {
  id: string;
  applicationId: string;
  interviewerId: string;
  candidateName?: string;
  requisitionTitle?: string;
  type: InterviewType;
  scheduledAt: string;
  durationMin: number;
  status: InterviewStatus;
  meetingLink?: string;
  hasScorecard: boolean;
  panelMemberIds: string[];
}

export enum InterviewType {
  Phone = 0,
  Video = 1,
  Technical = 2,
  OnSite = 3,
  Culture = 4,
}

export enum InterviewStatus {
  Scheduled = 0,
  Completed = 1,
  Cancelled = 2,
  NoShow = 3,
}

export interface AvailabilitySlot {
  id: string;
  interviewerId: string;
  startTime: string;
  endTime: string;
  isBooked: boolean;
}
```

- [ ] **Step 6: Create scorecard model**

Create `src/app/models/scorecard.model.ts`:

```typescript
export interface Scorecard {
  id: string;
  interviewId: string;
  interviewerId: string;
  ratings: Record<string, number>;
  recommendation: string;
  strengths: string;
  concerns: string;
  notes?: string;
  submittedAt?: string;
  isSubmitted: boolean;
}

export interface ScorecardSummary {
  totalInterviews: number;
  submittedCount: number;
  pendingCount: number;
  averageRatings: Record<string, number>;
  recommendationBreakdown: Record<string, number>;
  scorecards: ScorecardSummaryItem[];
}

export interface ScorecardSummaryItem {
  id: string;
  interviewId: string;
  interviewerId: string;
  recommendation: string;
  submittedAt?: string;
}
```

- [ ] **Step 7: Create remaining models**

Create `src/app/models/note.model.ts`:

```typescript
export interface Note {
  id: string;
  entityType: string;
  entityId: string;
  content: string;
  authorId: string;
  authorName: string;
  createdAt: string;
  updatedAt?: string;
}
```

Create `src/app/models/tag.model.ts`:

```typescript
export interface Tag {
  id: string;
  name: string;
  color: string;
}
```

Create `src/app/models/event-log.model.ts`:

```typescript
export interface CommunicationItem {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  actorId?: string;
  metadata?: Record<string, any>;
  createdAt: string;
  sourceType: 'event' | 'note';
}
```

- [ ] **Step 8: Create barrel export**

Create `src/app/models/index.ts`:

```typescript
export * from './common.model';
export * from './requisition.model';
export * from './candidate.model';
export * from './pipeline.model';
export * from './interview.model';
export * from './scorecard.model';
export * from './note.model';
export * from './tag.model';
export * from './event-log.model';
```

- [ ] **Step 9: Commit**

```bash
git add src/app/models/
git commit -m "feat(frontend): add shared model types for all Phase 1 modules"
```

---

### Task 2: Create shared services

- [ ] **Step 1: Create base API service**

Create `src/app/services/api.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  get<T>(path: string, params?: Record<string, any>): Observable<ApiResponse<T>> {
    return this.http.get<ApiResponse<T>>(`${this.baseUrl}${path}`, { params })
      .pipe(catchError(this.handleError));
  }

  post<T>(path: string, body: any): Observable<ApiResponse<T>> {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, body)
      .pipe(catchError(this.handleError));
  }

  put<T>(path: string, body: any): Observable<ApiResponse<T>> {
    return this.http.put<ApiResponse<T>>(`${this.baseUrl}${path}`, body)
      .pipe(catchError(this.handleError));
  }

  delete<T>(path: string): Observable<ApiResponse<T>> {
    return this.http.delete<ApiResponse<T>>(`${this.baseUrl}${path}`)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    const message = error.error?.error?.message || 'An unexpected error occurred';
    return throwError(() => new Error(message));
  }
}
```

- [ ] **Step 2: Create requisition service**

Create `src/app/services/requisition.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Requisition, PaginatedResult, RequisitionFilters } from '../models';

@Injectable({ providedIn: 'root' })
export class RequisitionService {
  constructor(private api: ApiService) {}

  list(filters?: RequisitionFilters): Observable<PaginatedResult<Requisition>> {
    return this.api.get<PaginatedResult<Requisition>>('/Requisitions', filters as any)
      .pipe(map(res => res.data!));
  }

  getById(id: string): Observable<Requisition> {
    return this.api.get<Requisition>(`/Requisitions/${id}`)
      .pipe(map(res => res.data!));
  }

  create(data: Partial<Requisition>): Observable<string> {
    return this.api.post<string>('/Requisitions', data)
      .pipe(map(res => res.data!));
  }

  update(id: string, data: Partial<Requisition>): Observable<void> {
    return this.api.put<void>(`/Requisitions/${id}`, data)
      .pipe(map(() => undefined));
  }

  submit(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/submit`, {})
      .pipe(map(() => undefined));
  }

  approve(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/approve`, {})
      .pipe(map(() => undefined));
  }

  reject(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/reject`, { reason })
      .pipe(map(() => undefined));
  }

  hold(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/hold`, {})
      .pipe(map(() => undefined));
  }

  close(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/close`, {})
      .pipe(map(() => undefined));
  }

  clone(id: string): Observable<string> {
    return this.api.post<string>(`/Requisitions/${id}/clone`, {})
      .pipe(map(res => res.data!));
  }
}
```

- [ ] **Step 3: Create remaining services**

Create `src/app/services/pipeline.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { PipelineStageGroup, PipelineApplicationCard } from '../models';

@Injectable({ providedIn: 'root' })
export class PipelineService {
  constructor(private api: ApiService) {}

  getByRequisition(requisitionId: string): Observable<PipelineStageGroup[]> {
    return this.api.get<PipelineStageGroup[]>(`/Pipeline/requisition/${requisitionId}`)
      .pipe(map(res => res.data!));
  }

  advance(applicationId: string, newStage: number): Observable<void> {
    return this.api.post<void>(`/Pipeline/applications/${applicationId}/advance`, { newStage })
      .pipe(map(() => undefined));
  }

  reject(applicationId: string, reason: string): Observable<void> {
    return this.api.post<void>(`/Pipeline/applications/${applicationId}/reject`, { reason })
      .pipe(map(() => undefined));
  }

  bulkAdvance(applicationIds: string[], newStage: number): Observable<void> {
    return this.api.post<void>('/Pipeline/bulk-advance', { applicationIds, newStage })
      .pipe(map(() => undefined));
  }
}
```

Create `src/app/services/candidate.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Candidate, PaginatedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class CandidateService {
  constructor(private api: ApiService) {}

  list(params?: { name?: string; page?: number; limit?: number }): Observable<PaginatedResult<Candidate>> {
    return this.api.get<PaginatedResult<Candidate>>('/Candidates', params as any)
      .pipe(map(res => res.data!));
  }

  getById(id: string): Observable<Candidate> {
    return this.api.get<Candidate>(`/Candidates/${id}`)
      .pipe(map(res => res.data!));
  }

  create(data: Partial<Candidate>): Observable<string> {
    return this.api.post<string>('/Candidates', data)
      .pipe(map(res => res.data!));
  }

  update(id: string, data: Partial<Candidate>): Observable<void> {
    return this.api.put<void>(`/Candidates/${id}`, data)
      .pipe(map(() => undefined));
  }

  apply(candidateId: string, requisitionId: string): Observable<string> {
    return this.api.post<string>('/Candidates/apply', { candidateId, requisitionId })
      .pipe(map(res => res.data!));
  }
}
```

Create `src/app/services/interview.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Interview, AvailabilitySlot, PaginatedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class InterviewService {
  constructor(private api: ApiService) {}

  getByApplication(applicationId: string): Observable<Interview[]> {
    return this.api.get<Interview[]>(`/Interviews/application/${applicationId}`)
      .pipe(map(res => res.data!));
  }

  getMy(params?: { status?: number; page?: number; limit?: number }): Observable<PaginatedResult<Interview>> {
    return this.api.get<PaginatedResult<Interview>>('/Interviews/my', params as any)
      .pipe(map(res => res.data!));
  }

  schedule(data: {
    applicationId: string;
    interviewerId: string;
    type: number;
    scheduledAt: string;
    durationMin: number;
    meetingLink?: string;
    panelMemberIds?: string[];
  }): Observable<string> {
    return this.api.post<string>('/Interviews', data)
      .pipe(map(res => res.data!));
  }

  reschedule(id: string, newScheduledAt: string): Observable<void> {
    return this.api.put<void>(`/Interviews/${id}/reschedule`, { newScheduledAt })
      .pipe(map(() => undefined));
  }

  cancel(id: string, reason?: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/cancel`, { reason })
      .pipe(map(() => undefined));
  }

  noShow(id: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/no-show`, {})
      .pipe(map(() => undefined));
  }

  complete(id: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/complete`, {})
      .pipe(map(() => undefined));
  }

  getAvailability(interviewerId: string, from: string, to: string): Observable<AvailabilitySlot[]> {
    return this.api.get<AvailabilitySlot[]>(`/Interviews/availability/${interviewerId}`, { from, to })
      .pipe(map(res => res.data!));
  }

  setAvailability(slots: { startTime: string; endTime: string }[]): Observable<void> {
    return this.api.post<void>('/Interviews/availability', { slots })
      .pipe(map(() => undefined));
  }
}
```

Create `src/app/services/scorecard.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Scorecard, ScorecardSummary } from '../models';

@Injectable({ providedIn: 'root' })
export class ScorecardService {
  constructor(private api: ApiService) {}

  getByInterview(interviewId: string): Observable<Scorecard> {
    return this.api.get<Scorecard>(`/Scorecards/interview/${interviewId}`)
      .pipe(map(res => res.data!));
  }

  getMy(): Observable<Scorecard[]> {
    return this.api.get<Scorecard[]>('/Scorecards/my')
      .pipe(map(res => res.data!));
  }

  getByApplication(applicationId: string): Observable<Scorecard[]> {
    return this.api.get<Scorecard[]>(`/Scorecards/application/${applicationId}`)
      .pipe(map(res => res.data!));
  }

  submit(id: string, data: {
    ratings: Record<string, number>;
    recommendation: string;
    strengths: string;
    concerns: string;
    notes?: string;
  }): Observable<void> {
    return this.api.post<void>(`/Scorecards/${id}/submit`, data)
      .pipe(map(() => undefined));
  }

  saveDraft(id: string, data: {
    ratings?: Record<string, number>;
    recommendation?: string;
    strengths?: string;
    concerns?: string;
    notes?: string;
  }): Observable<void> {
    return this.api.put<void>(`/Scorecards/${id}/draft`, data)
      .pipe(map(() => undefined));
  }

  getSummary(applicationId: string): Observable<ScorecardSummary> {
    return this.api.get<ScorecardSummary>(`/Scorecards/application/${applicationId}/summary`)
      .pipe(map(res => res.data!));
  }
}
```

Create `src/app/services/note.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Note } from '../models';

@Injectable({ providedIn: 'root' })
export class NoteService {
  constructor(private api: ApiService) {}

  list(entityType: string, entityId: string): Observable<Note[]> {
    return this.api.get<Note[]>('/Notes', { entityType, entityId })
      .pipe(map(res => res.data!));
  }

  create(data: { entityType: string; entityId: string; content: string }): Observable<string> {
    return this.api.post<string>('/Notes', data)
      .pipe(map(res => res.data!));
  }

  update(id: string, content: string): Observable<void> {
    return this.api.put<void>(`/Notes/${id}`, { content })
      .pipe(map(() => undefined));
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/Notes/${id}`)
      .pipe(map(() => undefined));
  }
}
```

Create `src/app/services/tag.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Tag } from '../models';

@Injectable({ providedIn: 'root' })
export class TagService {
  constructor(private api: ApiService) {}

  list(): Observable<Tag[]> {
    return this.api.get<Tag[]>('/Tags')
      .pipe(map(res => res.data!));
  }

  create(data: { name: string; color: string }): Observable<string> {
    return this.api.post<string>('/Tags', data)
      .pipe(map(res => res.data!));
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/Tags/${id}`)
      .pipe(map(() => undefined));
  }

  assignToCandidate(candidateId: string, tagId: string): Observable<void> {
    return this.api.post<void>(`/Tags/candidate/${candidateId}/assign`, { tagId })
      .pipe(map(() => undefined));
  }

  removeFromCandidate(candidateId: string, tagId: string): Observable<void> {
    return this.api.delete<void>(`/Tags/candidate/${candidateId}/tags/${tagId}`)
      .pipe(map(() => undefined));
  }
}
```

Create `src/app/services/communication.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CommunicationItem } from '../models';

@Injectable({ providedIn: 'root' })
export class CommunicationService {
  constructor(private api: ApiService) {}

  getFeed(entityId: string): Observable<CommunicationItem[]> {
    return this.api.get<CommunicationItem[]>(`/Communications/feed/${entityId}`)
      .pipe(map(res => res.data!));
  }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/app/services/
git commit -m "feat(frontend): add shared services for all Phase 1 modules"
```

---

### Task 3: Fix routing and navigation

- [ ] **Step 1: Update nav-menu with all Phase 1 links**

Replace `src/app/nav-menu/nav-menu.component.html`:

```html
<nav class="nav-container">
  <div class="nav-brand">
    <a routerLink="/">HyRe</a>
  </div>
  <ul class="nav-links">
    <li><a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">Home</a></li>
    <li><a routerLink="/requisitions" routerLinkActive="active">Requisitions</a></li>
    <li><a routerLink="/candidates" routerLinkActive="active">Candidates</a></li>
    <li><a routerLink="/interviewer" routerLinkActive="active">Interviews</a></li>
  </ul>
  <div class="nav-actions">
    <button class="btn-logout" (click)="logout()">Logout</button>
  </div>
</nav>
```

- [ ] **Step 2: Add candidate list route to app.module.ts**

In `src/app/app.module.ts`, add the route:

```typescript
{ path: 'candidates', component: CandidateListComponent, canActivate: [AuthGuard] },
```

And add `CandidateListComponent` to declarations.

- [ ] **Step 3: Commit**

```bash
git add src/app/nav-menu/nav-menu.component.html src/app/app.module.ts
git commit -m "feat(frontend): fix navigation and add candidate list route"
```

---

### Task 4: Rewrite requisitions list with real API, filter, and workflow

- [ ] **Step 1: Rewrite requisitions-list.ts**

Replace `src/app/requisitions/requisitions-list/requisitions-list.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RequisitionService } from '../../services/requisition.service';
import { Requisition, RequisitionStatus } from '../../models';

@Component({
  selector: 'app-requisitions-list',
  templateUrl: './requisitions-list.html',
  styleUrls: ['./requisitions-list.css']
})
export class RequisitionsListComponent implements OnInit {
  requisitions: Requisition[] = [];
  isLoading = true;
  errorMessage = '';

  // Filters
  statusFilter?: RequisitionStatus;
  departmentFilter = '';
  page = 1;
  limit = 20;
  totalCount = 0;

  statusOptions = [
    { value: undefined, label: 'All Statuses' },
    { value: RequisitionStatus.Draft, label: 'Draft' },
    { value: RequisitionStatus.PendingApproval, label: 'Pending Approval' },
    { value: RequisitionStatus.Open, label: 'Open' },
    { value: RequisitionStatus.OnHold, label: 'On Hold' },
    { value: RequisitionStatus.Closed, label: 'Closed' },
  ];

  constructor(
    private reqService: RequisitionService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadRequisitions();
  }

  loadRequisitions(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.reqService.list({
      status: this.statusFilter,
      department: this.departmentFilter || undefined,
      page: this.page,
      limit: this.limit,
    }).subscribe({
      next: (result) => {
        this.requisitions = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load requisitions';
        this.isLoading = false;
      }
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadRequisitions();
  }

  nextPage(): void {
    if (this.page * this.limit < this.totalCount) {
      this.page++;
      this.loadRequisitions();
    }
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadRequisitions();
    }
  }

  newRequisition(): void {
    this.router.navigate(['/requisitions/new']);
  }

  editRequisition(id: string): void {
    this.router.navigate(['/requisitions', id, 'edit']);
  }

  openPipeline(id: string): void {
    this.router.navigate(['/pipeline', id]);
  }

  // Workflow actions
  submitForApproval(req: Requisition): void {
    if (!confirm('Submit this requisition for approval?')) return;
    this.reqService.submit(req.id).subscribe({
      next: () => this.loadRequisitions(),
      error: (err) => alert(err.message)
    });
  }

  approveRequisition(req: Requisition): void {
    if (!confirm('Approve this requisition?')) return;
    this.reqService.approve(req.id).subscribe({
      next: () => this.loadRequisitions(),
      error: (err) => alert(err.message)
    });
  }

  rejectRequisition(req: Requisition): void {
    const reason = prompt('Reason for rejection:');
    if (reason === null) return;
    this.reqService.reject(req.id, reason).subscribe({
      next: () => this.loadRequisitions(),
      error: (err) => alert(err.message)
    });
  }

  holdRequisition(req: Requisition): void {
    if (!confirm('Put this requisition on hold?')) return;
    this.reqService.hold(req.id).subscribe({
      next: () => this.loadRequisitions(),
      error: (err) => alert(err.message)
    });
  }

  closeRequisition(req: Requisition): void {
    if (!confirm('Close this requisition?')) return;
    this.reqService.close(req.id).subscribe({
      next: () => this.loadRequisitions(),
      error: (err) => alert(err.message)
    });
  }

  cloneRequisition(req: Requisition): void {
    this.reqService.clone(req.id).subscribe({
      next: (newId) => this.router.navigate(['/requisitions', newId, 'edit']),
      error: (err) => alert(err.message)
    });
  }

  getStatusLabel(status: number): string {
    return RequisitionStatus[status] || 'Unknown';
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case RequisitionStatus.Draft: return 'badge-draft';
      case RequisitionStatus.PendingApproval: return 'badge-pending';
      case RequisitionStatus.Open: return 'badge-open';
      case RequisitionStatus.OnHold: return 'badge-on-hold';
      case RequisitionStatus.Closed: return 'badge-closed';
      default: return 'badge-default';
    }
  }
}
```

- [ ] **Step 2: Rewrite requisitions-list.html**

Replace `src/app/requisitions/requisitions-list/requisitions-list.html`:

```html
<div class="requisitions-container">
  <header class="req-header">
    <div class="header-text">
      <h2>Requisitions</h2>
      <p>Manage open jobs and track hiring progress.</p>
    </div>
    <div class="header-actions">
      <button class="primary-btn" (click)="newRequisition()">
        <lucide-icon name="plus"></lucide-icon> New Requisition
      </button>
    </div>
  </header>

  <!-- Filters -->
  <div class="filters-bar">
    <select [(ngModel)]="statusFilter" (change)="onFilterChange()">
      <option *ngFor="let opt of statusOptions" [ngValue]="opt.value">{{ opt.label }}</option>
    </select>
    <input type="text" placeholder="Department..." [(ngModel)]="departmentFilter"
           (keyup.enter)="onFilterChange()">
  </div>

  <div *ngIf="isLoading" class="loading-state">
    <div aria-busy="true">Loading requisitions...</div>
  </div>

  <div *ngIf="errorMessage" class="error-state">
    <p>{{ errorMessage }}</p>
    <button class="secondary-btn" (click)="loadRequisitions()">Retry</button>
  </div>

  <div *ngIf="!isLoading && !errorMessage" class="requisitions-grid">
    <div class="req-card" *ngFor="let req of requisitions">
      <div class="req-card-header">
        <h3 (click)="openPipeline(req.id)" class="clickable">{{ req.title }}</h3>
        <span class="badge" [ngClass]="getStatusBadgeClass(req.status)">
          {{ getStatusLabel(req.status) }}
        </span>
      </div>
      <div class="req-card-body">
        <div class="info-row">
          <span class="label">Department:</span>
          <span class="value">{{ req.department }}</span>
        </div>
        <div class="info-row">
          <span class="label">Headcount:</span>
          <span class="value">{{ req.headcount }}</span>
        </div>
        <div class="info-row" *ngIf="req.salaryMin && req.salaryMax">
          <span class="label">Salary:</span>
          <span class="value">{{ req.salaryMin | number }} - {{ req.salaryMax | number }}</span>
        </div>
      </div>
      <div class="req-card-footer">
        <button class="secondary-btn" (click)="openPipeline(req.id)">Pipeline</button>
        <button class="secondary-btn" (click)="editRequisition(req.id)">Edit</button>
        <button class="secondary-btn" (click)="cloneRequisition(req.id)">Clone</button>

        <!-- Workflow actions -->
        <button *ngIf="req.status === 0" class="primary-btn" (click)="submitForApproval(req)">
          Submit for Approval
        </button>
        <button *ngIf="req.status === 1" class="primary-btn" (click)="approveRequisition(req)">
          Approve
        </button>
        <button *ngIf="req.status === 1" class="danger-btn" (click)="rejectRequisition(req)">
          Reject
        </button>
        <button *ngIf="req.status === 2" class="secondary-btn" (click)="holdRequisition(req)">
          Hold
        </button>
        <button *ngIf="req.status === 2 || req.status === 3" class="danger-btn" (click)="closeRequisition(req)">
          Close
        </button>
      </div>
    </div>

    <div *ngIf="requisitions.length === 0" class="empty-state">
      <p>No requisitions found.</p>
    </div>
  </div>

  <!-- Pagination -->
  <div class="pagination" *ngIf="totalCount > limit">
    <button class="secondary-btn" (click)="prevPage()" [disabled]="page <= 1">Previous</button>
    <span>Page {{ page }} of {{ (totalCount / limit) | ceil }}</span>
    <button class="secondary-btn" (click)="nextPage()" [disabled]="page * limit >= totalCount">Next</button>
  </div>
</div>
```

- [ ] **Step 3: Commit**

```bash
git add src/app/requisitions/requisitions-list/
git commit -m "feat(frontend): rewrite requisitions list with real API, filters, and workflow actions"
```

---

### Task 5: Rewrite pipeline kanban with real API

- [ ] **Step 1: Rewrite pipeline-board.ts**

Replace `src/app/pipeline/pipeline-board/pipeline-board.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { PipelineService } from '../../services/pipeline.service';
import { RequisitionService } from '../../services/requisition.service';
import { PipelineStageGroup, PipelineApplicationCard } from '../../models';

@Component({
  selector: 'app-pipeline-board',
  templateUrl: './pipeline-board.html',
  styleUrls: ['./pipeline-board.css']
})
export class PipelineBoardComponent implements OnInit {
  requisitionId = '';
  requisitionTitle = '';
  stages: PipelineStageGroup[] = [];
  isLoading = true;
  errorMessage = '';
  selectedIds: Set<string> = new Set();

  stageNames: Record<number, string> = {
    0: 'Applied',
    1: 'Screened',
    2: 'Interview',
    3: 'Offer',
    4: 'Hired',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private pipelineService: PipelineService,
    private reqService: RequisitionService
  ) {}

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('id') || '';
    this.loadRequisition();
    this.loadPipeline();
  }

  loadRequisition(): void {
    this.reqService.getById(this.requisitionId).subscribe({
      next: (req) => this.requisitionTitle = req.title,
      error: () => this.requisitionTitle = 'Requisition'
    });
  }

  loadPipeline(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.pipelineService.getByRequisition(this.requisitionId).subscribe({
      next: (stages) => {
        this.stages = stages;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load pipeline';
        this.isLoading = false;
      }
    });
  }

  getStageApplications(stage: PipelineStageGroup): PipelineApplicationCard[] {
    return stage.applications || [];
  }

  getConnectedLists(): string[] {
    return this.stages.map(s => `stage-${s.stage}`);
  }

  onDrop(event: CdkDragDrop<PipelineApplicationCard[]>): void {
    if (event.previousContainer === event.container) return;

    const card = event.item.data as PipelineApplicationCard;
    const newStage = parseInt(event.container.id.replace('stage-', ''));

    // Optimistic UI
    moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);

    this.pipelineService.advance(card.applicationId, newStage).subscribe({
      error: (err) => {
        // Rollback on error
        this.loadPipeline();
        alert(err.message);
      }
    });
  }

  toggleSelect(id: string): void {
    if (this.selectedIds.has(id)) {
      this.selectedIds.delete(id);
    } else {
      this.selectedIds.add(id);
    }
  }

  bulkAdvance(newStage: number): void {
    if (this.selectedIds.size === 0) return;
    if (!confirm(`Move ${this.selectedIds.size} candidates to ${this.stageNames[newStage]}?`)) return;

    this.pipelineService.bulkAdvance(Array.from(this.selectedIds), newStage).subscribe({
      next: () => {
        this.selectedIds.clear();
        this.loadPipeline();
      },
      error: (err) => alert(err.message)
    });
  }

  rejectCandidate(card: PipelineApplicationCard): void {
    const reason = prompt('Reason for rejection:');
    if (reason === null) return;

    this.pipelineService.reject(card.applicationId, reason).subscribe({
      next: () => this.loadPipeline(),
      error: (err) => alert(err.message)
    });
  }

  viewCandidate(candidateId: string): void {
    this.router.navigate(['/candidates', candidateId]);
  }
}
```

- [ ] **Step 2: Rewrite pipeline-board.html**

Replace `src/app/pipeline/pipeline-board/pipeline-board.html`:

```html
<div class="pipeline-container">
  <header class="pipeline-header">
    <h2>{{ requisitionTitle }} — Pipeline</h2>
    <div class="header-actions" *ngIf="selectedIds.size > 0">
      <span>{{ selectedIds.size }} selected</span>
      <button class="secondary-btn" (click)="bulkAdvance(2)">Move to Interview</button>
      <button class="secondary-btn" (click)="bulkAdvance(3)">Move to Offer</button>
    </div>
  </header>

  <div *ngIf="isLoading" class="loading-state">
    <div aria-busy="true">Loading pipeline...</div>
  </div>

  <div *ngIf="errorMessage" class="error-state">
    <p>{{ errorMessage }}</p>
    <button class="secondary-btn" (click)="loadPipeline()">Retry</button>
  </div>

  <div *ngIf="!isLoading && !errorMessage" class="kanban-board">
    <div class="kanban-column" *ngFor="let stage of stages"
         [id]="'stage-' + stage.stage"
         cdkDropList
         [cdkDropListData]="stage.applications"
         [cdkDropListConnectedTo]="getConnectedLists()"
         (cdkDropListDropped)="onDrop($event)">
      <div class="column-header">
        <h3>{{ stageNames[stage.stage] || stage.stage }}</h3>
        <span class="count">{{ stage.applications?.length || 0 }}</span>
      </div>
      <div class="column-cards">
        <div class="candidate-card" *ngFor="let card of stage.applications"
             cdkDrag [cdkDragData]="card">
          <div class="card-select">
            <input type="checkbox" [checked]="selectedIds.has(card.applicationId)"
                   (change)="toggleSelect(card.applicationId)" (click)="$event.stopPropagation()">
          </div>
          <div class="card-content" (click)="viewCandidate(card.candidateId)">
            <h4>{{ card.candidateName }}</h4>
            <p class="card-email">{{ card.candidateEmail }}</p>
            <div class="card-meta">
              <span class="source-badge">{{ card.source }}</span>
              <span class="days-badge">{{ card.daysInStage }}d</span>
            </div>
          </div>
          <div class="card-actions">
            <button class="icon-btn" (click)="rejectCandidate(card); $event.stopPropagation()"
                    title="Reject">
              <lucide-icon name="x"></lucide-icon>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>

  <button class="secondary-btn back-btn" (click)="router.navigate(['/requisitions'])">
    Back to Requisitions
  </button>
</div>
```

- [ ] **Step 3: Commit**

```bash
git add src/app/pipeline/pipeline-board/
git commit -m "feat(frontend): rewrite pipeline kanban with real API, bulk ops, and reject"
```

---

### Task 6: Create candidate list and rewrite candidate detail

- [ ] **Step 1: Create candidate-list component**

Create `src/app/candidates/candidate-list/candidate-list.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CandidateService } from '../../services/candidate.service';
import { Candidate } from '../../models';

@Component({
  selector: 'app-candidate-list',
  templateUrl: './candidate-list.html',
  styleUrls: ['./candidate-list.css']
})
export class CandidateListComponent implements OnInit {
  candidates: Candidate[] = [];
  isLoading = true;
  errorMessage = '';
  nameFilter = '';
  page = 1;
  limit = 20;
  totalCount = 0;

  constructor(private candidateService: CandidateService, private router: Router) {}

  ngOnInit(): void {
    this.loadCandidates();
  }

  loadCandidates(): void {
    this.isLoading = true;
    this.candidateService.list({
      name: this.nameFilter || undefined,
      page: this.page,
      limit: this.limit,
    }).subscribe({
      next: (result) => {
        this.candidates = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message;
        this.isLoading = false;
      }
    });
  }

  viewCandidate(id: string): void {
    this.router.navigate(['/candidates', id]);
  }

  getSourceLabel(source: number): string {
    const labels = ['Direct', 'LinkedIn', 'Job Board', 'Referral', 'Agency', 'Headhunted'];
    return labels[source] || 'Unknown';
  }
}
```

Create `src/app/candidates/candidate-list/candidate-list.html`:

```html
<div class="candidate-list-container">
  <header>
    <h2>Candidates</h2>
    <div class="filters">
      <input type="text" placeholder="Search by name..." [(ngModel)]="nameFilter"
             (keyup.enter)="loadCandidates()">
      <button class="secondary-btn" (click)="loadCandidates()">Search</button>
    </div>
  </header>

  <div *ngIf="isLoading" aria-busy="true">Loading candidates...</div>
  <div *ngIf="errorMessage" class="error-state">{{ errorMessage }}</div>

  <table *ngIf="!isLoading && !errorMessage">
    <thead>
      <tr>
        <th>Name</th>
        <th>Email</th>
        <th>Source</th>
        <th>Applications</th>
        <th>Created</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let c of candidates" (click)="viewCandidate(c.id)" class="clickable">
        <td>{{ c.name }}</td>
        <td>{{ c.email }}</td>
        <td>{{ getSourceLabel(c.source) }}</td>
        <td>{{ c.applications?.length || 0 }}</td>
        <td>{{ c.createdAt | date:'mediumDate' }}</td>
      </tr>
    </tbody>
  </table>

  <div *ngIf="!isLoading && candidates.length === 0">No candidates found.</div>
</div>
```

- [ ] **Step 2: Rewrite candidate-detail.ts**

Replace `src/app/candidates/candidate-detail/candidate-detail.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CandidateService } from '../../services/candidate.service';
import { NoteService } from '../../services/note.service';
import { TagService } from '../../services/tag.service';
import { CommunicationService } from '../../services/communication.service';
import { Candidate, Note, Tag, CommunicationItem } from '../../models';

@Component({
  selector: 'app-candidate-detail',
  templateUrl: './candidate-detail.html',
  styleUrls: ['./candidate-detail.css']
})
export class CandidateDetailComponent implements OnInit {
  candidateId = '';
  candidate: Candidate | null = null;
  notes: Note[] = [];
  communications: CommunicationItem[] = [];
  availableTags: Tag[] = [];
  isLoading = true;
  errorMessage = '';
  newNoteContent = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private candidateService: CandidateService,
    private noteService: NoteService,
    private tagService: TagService,
    private communicationService: CommunicationService
  ) {}

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.paramMap.get('id') || '';
    this.loadCandidate();
    this.loadNotes();
    this.loadCommunications();
    this.loadTags();
  }

  loadCandidate(): void {
    this.isLoading = true;
    this.candidateService.getById(this.candidateId).subscribe({
      next: (c) => { this.candidate = c; this.isLoading = false; },
      error: (err) => { this.errorMessage = err.message; this.isLoading = false; }
    });
  }

  loadNotes(): void {
    this.noteService.list('candidate', this.candidateId).subscribe({
      next: (notes) => this.notes = notes,
      error: () => {}
    });
  }

  loadCommunications(): void {
    this.communicationService.getFeed(this.candidateId).subscribe({
      next: (items) => this.communications = items,
      error: () => {}
    });
  }

  loadTags(): void {
    this.tagService.list().subscribe({
      next: (tags) => this.availableTags = tags,
      error: () => {}
    });
  }

  addNote(): void {
    if (!this.newNoteContent.trim()) return;
    this.noteService.create({
      entityType: 'candidate',
      entityId: this.candidateId,
      content: this.newNoteContent
    }).subscribe({
      next: () => {
        this.newNoteContent = '';
        this.loadNotes();
      },
      error: (err) => alert(err.message)
    });
  }

  deleteNote(noteId: string): void {
    if (!confirm('Delete this note?')) return;
    this.noteService.delete(noteId).subscribe({
      next: () => this.loadNotes(),
      error: (err) => alert(err.message)
    });
  }

  assignTag(tagId: string): void {
    this.tagService.assignToCandidate(this.candidateId, tagId).subscribe({
      next: () => this.loadCandidate(),
      error: (err) => alert(err.message)
    });
  }

  removeTag(tagId: string): void {
    this.tagService.removeFromCandidate(this.candidateId, tagId).subscribe({
      next: () => this.loadCandidate(),
      error: (err) => alert(err.message)
    });
  }

  scheduleInterview(): void {
    if (this.candidate?.applications?.length) {
      const appId = this.candidate.applications[0].id;
      this.router.navigate(['/pipeline/schedule', appId]);
    }
  }

  getSourceLabel(source: number): string {
    const labels = ['Direct', 'LinkedIn', 'Job Board', 'Referral', 'Agency', 'Headhunted'];
    return labels[source] || 'Unknown';
  }

  getActionIcon(action: string): string {
    if (action.includes('created')) return 'user-plus';
    if (action.includes('stage')) return 'git-branch';
    if (action.includes('interview')) return 'calendar';
    if (action.includes('note')) return 'file-text';
    return 'activity';
  }
}
```

- [ ] **Step 3: Rewrite candidate-detail.html**

Replace `src/app/candidates/candidate-detail/candidate-detail.html`:

```html
<div class="candidate-detail" *ngIf="candidate">
  <div class="profile-sidebar">
    <div class="avatar">{{ candidate.name.charAt(0) }}</div>
    <h2>{{ candidate.name }}</h2>
    <p class="email">{{ candidate.email }}</p>
    <p *ngIf="candidate.phone">{{ candidate.phone }}</p>
    <p class="source">Source: {{ getSourceLabel(candidate.source) }}</p>

    <div class="tags-section">
      <h4>Tags</h4>
      <div class="tags-list">
        <span class="tag" *ngFor="let t of candidate.tags"
              [style.background-color]="t.tagColor">
          {{ t.tagName }}
          <button class="tag-remove" (click)="removeTag(t.tagId)">x</button>
        </span>
      </div>
      <select (change)="assignTag($event.target.value); $event.target.value = ''">
        <option value="">Add tag...</option>
        <option *ngFor="let t of availableTags" [value]="t.id">{{ t.name }}</option>
      </select>
    </div>

    <div class="actions">
      <button class="primary-btn" (click)="scheduleInterview()">Schedule Interview</button>
    </div>
  </div>

  <div class="profile-content">
    <!-- Activity Timeline -->
    <section class="timeline-section">
      <h3>Activity</h3>
      <div class="timeline">
        <div class="timeline-item" *ngFor="let item of communications">
          <div class="timeline-icon">
            <lucide-icon [name]="getActionIcon(item.action)"></lucide-icon>
          </div>
          <div class="timeline-body">
            <p>{{ item.action }}</p>
            <small>{{ item.createdAt | date:'medium' }}</small>
          </div>
        </div>
        <div *ngIf="communications.length === 0" class="empty-state">No activity yet.</div>
      </div>
    </section>

    <!-- Notes -->
    <section class="notes-section">
      <h3>Notes</h3>
      <div class="note-input">
        <textarea [(ngModel)]="newNoteContent" placeholder="Add a note..." rows="3"></textarea>
        <button class="primary-btn" (click)="addNote()">Add Note</button>
      </div>
      <div class="notes-list">
        <div class="note-card" *ngFor="let note of notes">
          <p>{{ note.content }}</p>
          <div class="note-meta">
            <small>{{ note.authorName }} - {{ note.createdAt | date:'medium' }}</small>
            <button class="icon-btn" (click)="deleteNote(note.id)">Delete</button>
          </div>
        </div>
      </div>
    </section>

    <!-- Applications -->
    <section class="applications-section" *ngIf="candidate.applications?.length">
      <h3>Applications</h3>
      <div class="application-card" *ngFor="let app of candidate.applications">
        <span>{{ app.requisitionTitle }}</span>
        <span class="badge">Stage {{ app.stage }}</span>
      </div>
    </section>
  </div>
</div>

<div *ngIf="isLoading" class="loading-state">Loading candidate...</div>
<div *ngIf="errorMessage" class="error-state">{{ errorMessage }}</div>
```

- [ ] **Step 4: Commit**

```bash
git add src/app/candidates/
git commit -m "feat(frontend): add candidate list and rewrite candidate detail with real API"
```

---

### Task 7: Rewrite interview components with real API

- [ ] **Step 1: Rewrite pending-interviews.ts**

Replace `src/app/interviewer/pending-interviews/pending-interviews.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { InterviewService } from '../../services/interview.service';
import { Interview, InterviewStatus, InterviewType } from '../../models';

@Component({
  selector: 'app-pending-interviews',
  templateUrl: './pending-interviews.html',
  styleUrls: ['./pending-interviews.css']
})
export class PendingInterviewsComponent implements OnInit {
  interviews: Interview[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private interviewService: InterviewService, private router: Router) {}

  ngOnInit(): void {
    this.loadInterviews();
  }

  loadInterviews(): void {
    this.isLoading = true;
    this.interviewService.getMy().subscribe({
      next: (result) => {
        this.interviews = result.items;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message;
        this.isLoading = false;
      }
    });
  }

  openScorecard(interviewId: string): void {
    this.router.navigate(['/interviewer/scorecard', interviewId]);
  }

  completeInterview(id: string): void {
    if (!confirm('Mark this interview as completed?')) return;
    this.interviewService.complete(id).subscribe({
      next: () => this.loadInterviews(),
      error: (err) => alert(err.message)
    });
  }

  cancelInterview(id: string): void {
    const reason = prompt('Reason for cancellation:');
    if (reason === null) return;
    this.interviewService.cancel(id, reason).subscribe({
      next: () => this.loadInterviews(),
      error: (err) => alert(err.message)
    });
  }

  rescheduleInterview(id: string): void {
    const newTime = prompt('New date/time (ISO format):');
    if (!newTime) return;
    this.interviewService.reschedule(id, newTime).subscribe({
      next: () => this.loadInterviews(),
      error: (err) => alert(err.message)
    });
  }

  getStatusLabel(status: number): string {
    return InterviewStatus[status] || 'Unknown';
  }

  getTypeLabel(type: number): string {
    return InterviewType[type] || 'Unknown';
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case InterviewStatus.Scheduled: return 'badge-scheduled';
      case InterviewStatus.Completed: return 'badge-completed';
      case InterviewStatus.Cancelled: return 'badge-cancelled';
      case InterviewStatus.NoShow: return 'badge-no-show';
      default: return '';
    }
  }
}
```

- [ ] **Step 2: Rewrite pending-interviews.html**

Replace `src/app/interviewer/pending-interviews/pending-interviews.html`:

```html
<div class="pending-interviews">
  <h2>My Interviews</h2>

  <div *ngIf="isLoading" aria-busy="true">Loading interviews...</div>
  <div *ngIf="errorMessage" class="error-state">{{ errorMessage }}</div>

  <div *ngIf="!isLoading && !errorMessage" class="interviews-list">
    <div class="interview-card" *ngFor="let interview of interviews">
      <div class="interview-date-box">
        <span class="date">{{ interview.scheduledAt | date:'d' }}</span>
        <span class="month">{{ interview.scheduledAt | date:'MMM' }}</span>
      </div>
      <div class="interview-info">
        <h3>{{ interview.candidateName || 'Candidate' }}</h3>
        <p>{{ interview.requisitionTitle || 'Requisition' }}</p>
        <p>{{ getTypeLabel(interview.type) }} | {{ interview.durationMin }}min</p>
        <p *ngIf="interview.meetingLink">
          <a [href]="interview.meetingLink" target="_blank">Join Meeting</a>
        </p>
      </div>
      <div class="interview-status">
        <span class="badge" [ngClass]="getStatusBadgeClass(interview.status)">
          {{ getStatusLabel(interview.status) }}
        </span>
      </div>
      <div class="interview-actions">
        <button *ngIf="interview.status === 0 && !interview.hasScorecard"
                class="primary-btn" (click)="openScorecard(interview.id)">
          Complete Scorecard
        </button>
        <button *ngIf="interview.status === 0"
                class="secondary-btn" (click)="completeInterview(interview.id)">
          Mark Complete
        </button>
        <button *ngIf="interview.status === 0"
                class="secondary-btn" (click)="rescheduleInterview(interview.id)">
          Reschedule
        </button>
        <button *ngIf="interview.status === 0"
                class="danger-btn" (click)="cancelInterview(interview.id)">
          Cancel
        </button>
      </div>
    </div>

    <div *ngIf="interviews.length === 0" class="empty-state">
      <p>No interviews scheduled.</p>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Rewrite scorecard-form.ts**

Replace `src/app/interviewer/scorecard-form/scorecard-form.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ScorecardService } from '../../services/scorecard.service';
import { Scorecard } from '../../models';

@Component({
  selector: 'app-scorecard-form',
  templateUrl: './scorecard-form.html',
  styleUrls: ['./scorecard-form.css']
})
export class ScorecardFormComponent implements OnInit {
  interviewId = '';
  scorecard: Scorecard | null = null;
  isLoading = true;
  isSubmitted = false;
  errorMessage = '';

  ratings = {
    technical: 3,
    communication: 3,
    problemSolving: 3,
    cultureFit: 3,
  };
  recommendation = 'Yes';
  strengths = '';
  concerns = '';
  notes = '';

  recommendations = ['StrongYes', 'Yes', 'No', 'StrongNo'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private scorecardService: ScorecardService
  ) {}

  ngOnInit(): void {
    this.interviewId = this.route.snapshot.paramMap.get('interviewId') || '';
    this.loadScorecard();
  }

  loadScorecard(): void {
    this.scorecardService.getByInterview(this.interviewId).subscribe({
      next: (sc) => {
        this.scorecard = sc;
        this.isSubmitted = sc.isSubmitted;
        if (sc.isSubmitted) {
          this.ratings = sc.ratings as any;
          this.recommendation = sc.recommendation;
          this.strengths = sc.strengths;
          this.concerns = sc.concerns;
          this.notes = sc.notes || '';
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  autoSave(): void {
    if (!this.scorecard || this.isSubmitted) return;
    this.scorecardService.saveDraft(this.scorecard.id, {
      ratings: this.ratings,
      recommendation: this.recommendation,
      strengths: this.strengths,
      concerns: this.concerns,
      notes: this.notes,
    }).subscribe({ error: () => {} });
  }

  submit(): void {
    if (!this.scorecard) return;
    if (!this.strengths.trim() || !this.concerns.trim()) {
      alert('Strengths and Concerns are required.');
      return;
    }

    this.scorecardService.submit(this.scorecard.id, {
      ratings: this.ratings,
      recommendation: this.recommendation,
      strengths: this.strengths,
      concerns: this.concerns,
      notes: this.notes,
    }).subscribe({
      next: () => this.router.navigate(['/interviewer']),
      error: (err) => alert(err.message)
    });
  }
}
```

- [ ] **Step 4: Rewrite scorecard-form.html**

Replace `src/app/interviewer/scorecard-form/scorecard-form.html`:

```html
<div class="scorecard-form" *ngIf="!isLoading">
  <h2>{{ isSubmitted ? 'Scorecard (Submitted)' : 'Complete Scorecard' }}</h2>

  <div *ngIf="errorMessage" class="error-state">{{ errorMessage }}</div>

  <form [class.readonly]="isSubmitted">
    <div class="rating-group">
      <h3>Ratings</h3>
      <div class="rating-row" *ngFor="let dim of ['technical', 'communication', 'problemSolving', 'cultureFit']">
        <label>{{ dim | titlecase }}</label>
        <div class="rating-scale">
          <button type="button" *ngFor="let v of [1,2,3,4,5]"
                  [class.active]="ratings[dim] === v"
                  (click)="!isSubmitted && (ratings[dim] = v); autoSave()"
                  [disabled]="isSubmitted">
            {{ v }}
          </button>
        </div>
      </div>
    </div>

    <div class="form-group">
      <label>Recommendation</label>
      <select [(ngModel)]="recommendation" name="recommendation"
              [disabled]="isSubmitted" (change)="autoSave()">
        <option *ngFor="let r of recommendations" [value]="r">{{ r }}</option>
      </select>
    </div>

    <div class="form-group">
      <label>Strengths *</label>
      <textarea [(ngModel)]="strengths" name="strengths" rows="3"
                [readonly]="isSubmitted" (blur)="autoSave()"></textarea>
    </div>

    <div class="form-group">
      <label>Concerns *</label>
      <textarea [(ngModel)]="concerns" name="concerns" rows="3"
                [readonly]="isSubmitted" (blur)="autoSave()"></textarea>
    </div>

    <div class="form-group">
      <label>Notes</label>
      <textarea [(ngModel)]="notes" name="notes" rows="2"
                [readonly]="isSubmitted" (blur)="autoSave()"></textarea>
    </div>

    <div class="form-actions" *ngIf="!isSubmitted">
      <button type="button" class="primary-btn" (click)="submit()">Submit Scorecard</button>
      <button type="button" class="secondary-btn" (click)="router.navigate(['/interviewer'])">Cancel</button>
    </div>

    <div class="form-actions" *ngIf="isSubmitted">
      <button type="button" class="secondary-btn" (click)="router.navigate(['/interviewer'])">Back</button>
    </div>
  </form>
</div>

<div *ngIf="isLoading" class="loading-state">Loading scorecard...</div>
```

- [ ] **Step 5: Rewrite schedule-interview.ts**

Replace `src/app/candidates/schedule-interview/schedule-interview.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { InterviewService } from '../../services/interview.service';
import { InterviewType } from '../../models';

@Component({
  selector: 'app-schedule-interview',
  templateUrl: './schedule-interview.html',
  styleUrls: ['./schedule-interview.css']
})
export class ScheduleInterviewComponent implements OnInit {
  applicationId = '';
  interviewerId = '';
  type = InterviewType.Video;
  scheduledAt = '';
  durationMin = 60;
  meetingLink = '';
  isSubmitting = false;

  interviewTypes = [
    { value: InterviewType.Phone, label: 'Phone Screen' },
    { value: InterviewType.Video, label: 'Video Call' },
    { value: InterviewType.Technical, label: 'Technical Challenge' },
    { value: InterviewType.OnSite, label: 'Onsite' },
    { value: InterviewType.Culture, label: 'Culture Fit' },
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private interviewService: InterviewService
  ) {}

  ngOnInit(): void {
    this.applicationId = this.route.snapshot.paramMap.get('candidateId') || '';
  }

  submit(): void {
    if (!this.interviewerId || !this.scheduledAt) {
      alert('Please fill in interviewer and date/time.');
      return;
    }

    this.isSubmitting = true;
    this.interviewService.schedule({
      applicationId: this.applicationId,
      interviewerId: this.interviewerId,
      type: this.type,
      scheduledAt: new Date(this.scheduledAt).toISOString(),
      durationMin: this.durationMin,
      meetingLink: this.meetingLink || undefined,
    }).subscribe({
      next: () => this.router.navigate(['/interviewer']),
      error: (err) => {
        alert(err.message);
        this.isSubmitting = false;
      }
    });
  }
}
```

- [ ] **Step 6: Rewrite schedule-interview.html**

Replace `src/app/candidates/schedule-interview/schedule-interview.html`:

```html
<div class="schedule-form">
  <h2>Schedule Interview</h2>

  <form (ngSubmit)="submit()">
    <div class="form-group">
      <label>Interviewer ID</label>
      <input type="text" [(ngModel)]="interviewerId" name="interviewerId" required
             placeholder="Enter interviewer user ID">
    </div>

    <div class="form-group">
      <label>Interview Type</label>
      <select [(ngModel)]="type" name="type">
        <option *ngFor="let t of interviewTypes" [ngValue]="t.value">{{ t.label }}</option>
      </select>
    </div>

    <div class="form-group">
      <label>Date & Time</label>
      <input type="datetime-local" [(ngModel)]="scheduledAt" name="scheduledAt" required>
    </div>

    <div class="form-group">
      <label>Duration (minutes)</label>
      <input type="number" [(ngModel)]="durationMin" name="durationMin" min="15" max="480">
    </div>

    <div class="form-group">
      <label>Meeting Link (optional)</label>
      <input type="url" [(ngModel)]="meetingLink" name="meetingLink" placeholder="https://...">
    </div>

    <div class="form-actions">
      <button type="submit" class="primary-btn" [disabled]="isSubmitting">
        {{ isSubmitting ? 'Scheduling...' : 'Schedule Interview' }}
      </button>
      <button type="button" class="secondary-btn" (click)="router.navigate(['/interviewer'])">Cancel</button>
    </div>
  </form>
</div>
```

- [ ] **Step 7: Commit**

```bash
git add src/app/interviewer/ src/app/candidates/schedule-interview/
git commit -m "feat(frontend): rewrite interview components with real API, reschedule/cancel/complete"
```

---

### Task 8: Update app.module.ts with all new components

- [ ] **Step 1: Update declarations and routes**

In `src/app/app.module.ts`, add imports and declarations for:
- `CandidateListComponent`
- Add route: `{ path: 'candidates', component: CandidateListComponent, canActivate: [AuthGuard] }`

- [ ] **Step 2: Verify build**

Run: `cd src/Web/ClientApp && npx ng build`
Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add src/app/app.module.ts
git commit -m "feat(frontend): register all new components in app.module"
```

---

### Task 9: Remove all mock data and verify end-to-end

- [ ] **Step 1: Verify no mock data remains**

Run: `grep -r "loadMockData\|setTimeout.*mock\|mock.*data" src/Web/ClientApp/src/app/ --include="*.ts"`
Expected: No results (all mock methods should be gone from the rewritten components).

- [ ] **Step 2: Verify no wrong API URLs remain**

Run: `grep -r "/api/v1/" src/Web/ClientApp/src/app/ --include="*.ts"`
Expected: No results.

- [ ] **Step 3: Verify build and lint pass**

Run: `cd src/Web/ClientApp && npx ng build && npx ng lint`
Expected: Build succeeded, no lint errors.

- [ ] **Step 4: Final commit if needed**

```bash
git add -A
git commit -m "fix(frontend): remove remaining mock data and wrong API URLs"
```

---

*End of frontend rewrite plan*
