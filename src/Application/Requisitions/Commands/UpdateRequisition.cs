using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record UpdateRequisition(
    Guid Id,
    string Title,
    string Department,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount
) : IRequest<Result>;

public class UpdateRequisitionHandler : IRequestHandler<UpdateRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public UpdateRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Draft)
            return Result.Failure("Only draft requisitions can be edited.");

        requisition.Title = request.Title;
        requisition.Department = request.Department;
        requisition.JdText = request.JdText;
        requisition.SalaryMin = request.SalaryMin;
        requisition.SalaryMax = request.SalaryMax;
        requisition.Headcount = request.Headcount;

        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
