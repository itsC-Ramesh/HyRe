using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

public record RequisitionDto(
    Guid Id,
    string Title,
    string Department,
    string OwnerId,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount,
    RequisitionStatus Status,
    Dictionary<ApplicationStage, int> ApplicationCountByStage,
    DateTimeOffset Created,
    DateTimeOffset LastModified
);
