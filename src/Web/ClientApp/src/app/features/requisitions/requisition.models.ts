export type RequisitionStatus = 'draft' | 'pending_approval' | 'open' | 'on_hold' | 'closed';

export interface RequisitionDto {
  id: string;
  title: string;
  department: string;
  ownerId: string;
  jdText: string;
  salaryMin: number | null;
  salaryMax: number | null;
  headcount: number;
  status: RequisitionStatus;
  applicationCountByStage: Record<string, number>;
  created: string;
  lastModified: string;
}

export interface PaginatedRequisitions {
  items: RequisitionDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
}
