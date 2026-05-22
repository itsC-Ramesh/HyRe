using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

public interface IScorecardRepository
{
    Task<Scorecard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Scorecard?> GetByInterviewIdAsync(Guid interviewId, CancellationToken ct = default);
    Task<PaginatedList<Scorecard>> GetByInterviewerAsync(string interviewerId, int page, int limit, CancellationToken ct = default);
    Task<bool> AllSubmittedForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task AddAsync(Scorecard scorecard, CancellationToken ct = default);
}
