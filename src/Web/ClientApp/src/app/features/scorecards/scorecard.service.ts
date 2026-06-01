import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { ApiService } from '../../core/http/api.service';
import {
  ScorecardDto,
  PaginatedScorecards,
  ScorecardSummaryDto,
  SaveDraftPayload,
  SubmitScorecardPayload,
} from './scorecard.models';

@Injectable({ providedIn: 'root' })
export class ScorecardService {
  private api = inject(ApiService);

  getByInterview(interviewId: string): Observable<ScorecardDto> {
    return this.api.get<ScorecardDto>(`/Scorecards/interview/${interviewId}`);
  }

  getMy(page = 1, limit = 20): Observable<PaginatedScorecards> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());
    return this.api.get<PaginatedScorecards>('/Scorecards/my', params);
  }

  getByApplication(applicationId: string): Observable<ScorecardDto[]> {
    return this.api.get<ScorecardDto[]>(`/Scorecards/application/${applicationId}`);
  }

  getSummary(applicationId: string): Observable<ScorecardSummaryDto> {
    return this.api.get<ScorecardSummaryDto>(`/Scorecards/application/${applicationId}/summary`);
  }

  saveDraft(id: string, payload: SaveDraftPayload): Observable<void> {
    return this.api.put<void>(`/Scorecards/${id}/draft`, payload);
  }

  submit(id: string, payload: SubmitScorecardPayload): Observable<void> {
    return this.api.post<void>(`/Scorecards/${id}/submit`, payload);
  }
}
