using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesUpdate)]
public record UpdateCandidate(
    Guid Id,
    string Name,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail
) : IRequest<Result>;

public class UpdateCandidateHandler : IRequestHandler<UpdateCandidate, Result>
{
    private readonly ICandidateRepository _repository;

    public UpdateCandidateHandler(ICandidateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateCandidate request, CancellationToken ct)
    {
        var candidate = await _repository.GetByIdAsync(request.Id, ct);
        if (candidate is null)
            return Result.Failure("Candidate not found.");

        candidate.Name = request.Name;
        candidate.Phone = request.Phone;
        candidate.Source = request.Source;
        candidate.SourceDetail = request.SourceDetail;

        await _repository.UpdateAsync(candidate, ct);
        return Result.Success();
    }
}
