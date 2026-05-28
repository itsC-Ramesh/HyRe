export type CandidateSource = 'direct' | 'linkedin' | 'job_board' | 'referral' | 'agency' | 'headhunted';

export interface CandidateDto {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  source: CandidateSource;
  sourceDetail: string | null;
  resumeDocId: string | null;
  applications: CandidateApplicationSummary[];
  created: string;
}

export interface CandidateApplicationSummary {
  applicationId: string;
  requisitionId: string;
  requisitionTitle: string;
  stage: string;
  created: string;
}

export interface PaginatedCandidates {
  items: CandidateDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
}
