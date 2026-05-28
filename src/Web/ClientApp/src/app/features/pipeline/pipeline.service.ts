import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/http/api.service';
import { PipelineDto, PipelineApplicationCard } from './pipeline.models';

@Injectable({ providedIn: 'root' })
export class PipelineService {
  private api = inject(ApiService);

  getByRequisition(requisitionId: string): Observable<PipelineDto> {
    return this.api.get<PipelineDto>(`/Pipeline/requisition/${requisitionId}`);
  }

  getApplication(id: string): Observable<PipelineApplicationCard> {
    return this.api.get<PipelineApplicationCard>(`/Pipeline/applications/${id}`);
  }

  advance(id: string, newStage: string): Observable<void> {
    return this.api.post<void>(`/Pipeline/applications/${id}/advance`, { newStage });
  }

  reject(id: string, reason?: string): Observable<void> {
    return this.api.post<void>(`/Pipeline/applications/${id}/reject`, { reason });
  }

  bulkAdvance(applicationIds: string[], newStage: string): Observable<void> {
    return this.api.post<void>('/Pipeline/applications/bulk-advance', { applicationIds, newStage });
  }
}
