import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/http/api.service';
import { InterviewDto, AvailabilitySlot } from './interview.models';

@Injectable({ providedIn: 'root' })
export class InterviewService {
  private api = inject(ApiService);

  getByApplication(applicationId: string): Observable<InterviewDto[]> {
    return this.api.get<InterviewDto[]>(`/Interviews/application/${applicationId}`);
  }

  getMyInterviews(): Observable<InterviewDto[]> {
    return this.api.get<InterviewDto[]>('/Interviews/my');
  }

  schedule(command: {
    applicationId: string;
    interviewerId: string;
    type: string;
    scheduledAt: string;
    durationMin: number;
    meetingLink?: string;
  }): Observable<string> {
    return this.api.post<string>('/Interviews', command);
  }

  reschedule(id: string, scheduledAt: string, reason: string): Observable<void> {
    return this.api.put<void>(`/Interviews/${id}/reschedule`, { scheduledAt, reason });
  }

  cancel(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/cancel`, { reason });
  }

  markNoShow(id: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/no-show`, {});
  }

  markCompleted(id: string): Observable<void> {
    return this.api.post<void>(`/Interviews/${id}/complete`, {});
  }

  getAvailability(interviewerId: string): Observable<AvailabilitySlot[]> {
    return this.api.get<AvailabilitySlot[]>(`/Interviews/availability/${interviewerId}`);
  }

  setAvailability(slots: { dayOfWeek: number; startTime: string; endTime: string }[]): Observable<void> {
    return this.api.post<void>('/Interviews/availability', { slots });
  }
}
