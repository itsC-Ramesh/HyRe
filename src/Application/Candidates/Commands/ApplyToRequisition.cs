using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesCreate)]
public record ApplyToRequisition(Guid CandidateId, Guid RequisitionId) : IRequest<Result<Guid>>;

public class ApplyToRequisitionHandler : IRequestHandler<ApplyToRequisition, Result<Guid>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApplicationRepository _applicationRepository;

    public ApplyToRequisitionHandler(
        ICandidateRepository candidateRepository,
        IRequisitionRepository requisitionRepository,
        IApplicationRepository applicationRepository)
    {
        _candidateRepository = candidateRepository;
        _requisitionRepository = requisitionRepository;
        _applicationRepository = applicationRepository;
    }

    public async Task<Result<Guid>> Handle(ApplyToRequisition request, CancellationToken ct)
    {
        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId, ct);
        if (candidate is null)
            return Result.Failure<Guid>("Candidate not found.");

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, ct);
        if (requisition is null)
            return Result.Failure<Guid>("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Open)
            return Result.Failure<Guid>("Can only apply to open requisitions.");

        var duplicate = await _applicationRepository.ExistsDuplicateAsync(
            request.CandidateId, request.RequisitionId, ct);
        if (duplicate)
            return Result.Failure<Guid>("Candidate already applied to this requisition.");

        var application = new JobApplication
        {
            CandidateId = request.CandidateId,
            RequisitionId = request.RequisitionId,
            Stage = ApplicationStage.Applied
        };

        await _applicationRepository.AddAsync(application, ct);
        return Result.Success(application.Id);
    }
}
