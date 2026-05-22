using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record SubmitForApproval(Guid Id) : IRequest<Result>;

public class SubmitForApprovalHandler : IRequestHandler<SubmitForApproval, Result>
{
    private readonly IRequisitionRepository _repository;

    public SubmitForApprovalHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitForApproval request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Draft)
            return Result.Failure("Only draft requisitions can be submitted for approval.");

        requisition.Status = RequisitionStatus.PendingApproval;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
