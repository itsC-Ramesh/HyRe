using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

public interface IInterviewRepository
{
    Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedList<Interview>> GetByApplicationAsync(Guid applicationId, int page, int limit, CancellationToken ct = default);
    Task<PaginatedList<Interview>> GetByInterviewerAsync(string interviewerId, InterviewStatus? statusFilter, int page, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Interview>> GetUpcomingAsync(TimeSpan within, CancellationToken ct = default);
    Task<IReadOnlyList<Interview>> GetPastDueForScorecardAsync(CancellationToken ct = default);
}
