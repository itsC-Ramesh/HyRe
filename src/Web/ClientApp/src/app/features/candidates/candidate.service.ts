import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { ApiService } from '../../core/http/api.service';
import { CandidateDto, PaginatedCandidates } from './candidate.models';

@Injectable({ providedIn: 'root' })
export class CandidateService {
  private api = inject(ApiService);

  getAll(name?: string, page = 1, limit = 20): Observable<PaginatedCandidates> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());
    if (name) params = params.set('name', name);
    return this.api.get<PaginatedCandidates>('/Candidates', params);
  }

  getById(id: string): Observable<CandidateDto> {
    return this.api.get<CandidateDto>(`/Candidates/${id}`);
  }

  create(command: { name: string; email: string; phone?: string; source: string; sourceDetail?: string }): Observable<string> {
    return this.api.post<string>('/Candidates', command);
  }

  update(id: string, body: { name: string; phone?: string; source: string; sourceDetail?: string }): Observable<void> {
    return this.api.put<void>(`/Candidates/${id}`, body);
  }

  applyToRequisition(candidateId: string, requisitionId: string): Observable<void> {
    return this.api.post<void>(`/Candidates/${candidateId}/apply`, { requisitionId });
  }
}
