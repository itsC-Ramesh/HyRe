export type Recommendation = 'StrongYes' | 'Yes' | 'No' | 'StrongNo';

export interface ScorecardRatings {
  technical: number;
  communication: number;
  problemSolving: number;
  cultureFit: number;
}

export interface ScorecardDto {
  id: string;
  interviewId: string;
  interviewerId: string;
  ratings: ScorecardRatings;
  recommendation: Recommendation;
  strengths: string;
  concerns: string;
  notes: string | null;
  submittedAt: string | null;
  isSubmitted: boolean;
}

export interface PaginatedScorecards {
  items: ScorecardDto[];
  page: number;
  limit: number;
  totalCount: number;
  totalPages: number;
}

export interface ScorecardSummaryDto {
  totalInterviews: number;
  submittedCount: number;
  pendingCount: number;
  averageRatings: Record<string, number>;
  recommendationBreakdown: Record<string, number>;
  scorecards: ScorecardSummaryItemDto[];
}

export interface ScorecardSummaryItemDto {
  id: string;
  interviewId: string;
  interviewerId: string;
  recommendation: string;
  submittedAt: string | null;
}

export interface SaveDraftPayload {
  ratings?: Record<string, number> | null;
  recommendation?: string | null;
  strengths?: string | null;
  concerns?: string | null;
  notes?: string | null;
}

export interface SubmitScorecardPayload {
  ratings: Record<string, number>;
  recommendation: string;
  strengths: string;
  concerns: string;
  notes?: string | null;
}
