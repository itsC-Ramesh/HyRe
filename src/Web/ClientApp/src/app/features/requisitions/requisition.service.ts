import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { ApiService } from '../../core/http/api.service';
import { RequisitionDto, PaginatedRequisitions } from './requisition.models';

@Injectable({ providedIn: 'root' })
export class RequisitionService {
  private api = inject(ApiService);

  getAll(status?: string, department?: string, page = 1, limit = 20): Observable<PaginatedRequisitions> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());
    if (status) params = params.set('status', status);
    if (department) params = params.set('department', department);
    return this.api.get<PaginatedRequisitions>('/Requisitions', params);
  }

  getById(id: string): Observable<RequisitionDto> {
    return this.api.get<RequisitionDto>(`/Requisitions/${id}`);
  }

  create(command: { title: string; department: string; jdText: string; salaryMin?: number; salaryMax?: number; headcount: number }): Observable<string> {
    return this.api.post<string>('/Requisitions', command);
  }

  update(id: string, body: { title: string; department: string; jdText: string; salaryMin?: number; salaryMax?: number; headcount: number }): Observable<void> {
    return this.api.put<void>(`/Requisitions/${id}`, body);
  }

  submit(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/submit`, {});
  }

  approve(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/approve`, {});
  }

  reject(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/reject`, { reason });
  }

  hold(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/hold`, {});
  }

  close(id: string): Observable<void> {
    return this.api.post<void>(`/Requisitions/${id}/close`, {});
  }

  clone(id: string): Observable<string> {
    return this.api.post<string>(`/Requisitions/${id}/clone`, {});
  }
}
