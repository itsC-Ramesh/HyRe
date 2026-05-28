export interface PipelineDto {
  requisitionId: string;
  requisitionTitle: string;
  stages: PipelineStageGroup[];
}

export interface PipelineStageGroup {
  stage: string;
  applications: PipelineApplicationCard[];
}

export interface PipelineApplicationCard {
  applicationId: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  stage: string;
  daysInStage: number;
  created: string;
}
