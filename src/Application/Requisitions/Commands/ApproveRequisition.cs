using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Roles = Roles.HrAdmin)]
public record ApproveRequisition(Guid Id) : IRequest<Result>;

public class ApproveRequisitionHandler : IRequestHandler<ApproveRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public ApproveRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.PendingApproval)
            return Result.Failure("Only pending requisitions can be approved.");

        requisition.Status = RequisitionStatus.Open;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
