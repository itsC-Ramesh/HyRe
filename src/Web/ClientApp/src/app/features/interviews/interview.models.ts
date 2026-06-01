export type InterviewType = 'phone' | 'video' | 'technical' | 'onsite' | 'culture';
export type InterviewStatus = 'scheduled' | 'completed' | 'cancelled' | 'no_show';

export interface InterviewDto {
  id: string;
  applicationId: string;
  candidateName: string;
  requisitionTitle: string;
  interviewerId: string;
  interviewerName: string;
  type: InterviewType;
  scheduledAt: string;
  durationMin: number;
  status: InterviewStatus;
  meetingLink: string | null;
  created: string;
}

export interface AvailabilitySlot {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}
