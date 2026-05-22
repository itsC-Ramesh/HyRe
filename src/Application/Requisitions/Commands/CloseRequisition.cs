using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record CloseRequisition(Guid Id) : IRequest<Result>;

public class CloseRequisitionHandler : IRequestHandler<CloseRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public CloseRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CloseRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status is not (RequisitionStatus.Open or RequisitionStatus.OnHold))
            return Result.Failure("Only open or on-hold requisitions can be closed.");

        requisition.Status = RequisitionStatus.Closed;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
