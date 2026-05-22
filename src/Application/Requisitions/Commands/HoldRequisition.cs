using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record HoldRequisition(Guid Id) : IRequest<Result>;

public class HoldRequisitionHandler : IRequestHandler<HoldRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public HoldRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(HoldRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Open)
            return Result.Failure("Only open requisitions can be put on hold.");

        requisition.Status = RequisitionStatus.OnHold;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
