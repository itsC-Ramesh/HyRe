using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsCreate)]
public record CreateRequisition(
    string Title,
    string Department,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount
) : IRequest<Result<Guid>>;

public class CreateRequisitionHandler : IRequestHandler<CreateRequisition, Result<Guid>>
{
    private readonly IRequisitionRepository _repository;
    private readonly IUser _user;

    public CreateRequisitionHandler(IRequisitionRepository repository, IUser user)
    {
        _repository = repository;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateRequisition request, CancellationToken ct)
    {
        var requisition = new Requisition
        {
            Title = request.Title,
            Department = request.Department,
            OwnerId = _user.Id!,
            JdText = request.JdText,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Headcount = request.Headcount,
            Status = RequisitionStatus.Draft
        };

        await _repository.AddAsync(requisition, ct);
        return Result.Success(requisition.Id);
    }
}
