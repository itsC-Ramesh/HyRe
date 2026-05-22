using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Queries;

public record CandidateDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail,
    Guid? ResumeDocId,
    List<CandidateApplicationSummary> Applications,
    DateTimeOffset Created
);

public record CandidateApplicationSummary(
    Guid ApplicationId,
    Guid RequisitionId,
    string RequisitionTitle,
    ApplicationStage Stage,
    DateTimeOffset Created
);
