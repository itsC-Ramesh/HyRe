using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Roles = Roles.HrAdmin)]
public record RejectRequisition(Guid Id, string Reason) : IRequest<Result>;

public class RejectRequisitionHandler : IRequestHandler<RejectRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public RejectRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.PendingApproval)
            return Result.Failure("Only pending requisitions can be rejected.");

        requisition.Status = RequisitionStatus.Draft;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
