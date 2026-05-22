using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Queries;

public record PipelineDto(
    Guid RequisitionId,
    string RequisitionTitle,
    List<PipelineStageGroup> Stages
);

public record PipelineStageGroup(
    ApplicationStage Stage,
    List<PipelineApplicationCard> Applications
);

public record PipelineApplicationCard(
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    ApplicationStage Stage,
    int DaysInStage,
    DateTimeOffset Created
);

public record ApplicationDetailDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    Guid RequisitionId,
    string RequisitionTitle,
    ApplicationStage Stage,
    string? RejectionReason,
    List<ApplicationInterviewSummary> Interviews,
    DateTimeOffset Created
);

public record ApplicationInterviewSummary(
    Guid InterviewId,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    InterviewStatus Status,
    bool HasScorecard
);
