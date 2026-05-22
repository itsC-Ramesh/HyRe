using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesCreate)]
public record CreateCandidate(
    string Name,
    string Email,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail
) : IRequest<Result<Guid>>;

public class CreateCandidateHandler : IRequestHandler<CreateCandidate, Result<Guid>>
{
    private readonly ICandidateRepository _repository;

    public CreateCandidateHandler(ICandidateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateCandidate request, CancellationToken ct)
    {
        var exists = await _repository.ExistsWithEmailAsync(request.Email, ct);
        if (exists)
            return Result.Failure<Guid>("A candidate with this email already exists.");

        var candidate = new Candidate
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            Source = request.Source,
            SourceDetail = request.SourceDetail
        };

        candidate.AddDomainEvent(new CandidateCreatedEvent(candidate.Id, candidate.Email, null));

        await _repository.AddAsync(candidate, ct);
        return Result.Success(candidate.Id);
    }
}
