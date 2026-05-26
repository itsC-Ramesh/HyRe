using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Queries;

public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    string CandidateName,
    string RequisitionTitle,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    int DurationMin,
    InterviewStatus Status,
    string? MeetingLink,
    bool HasScorecard,
    List<string> PanelMemberIds
);
