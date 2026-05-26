using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Requisitions.Commands.CloneRequisition;

public class CloneRequisitionCommandHandler(IRequisitionRepository repository, IUser currentUser) : IRequestHandler<CloneRequisitionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CloneRequisitionCommand request, CancellationToken ct)
    {
        var original = await repository.GetByIdAsync(request.RequisitionId, ct);

        if (original == null)
            return Result<Guid>.Failure("Requisition not found.");

        var clone = new Requisition
        {
            Title = $"{original.Title} (Copy)",
            Department = original.Department,
            JdText = original.JdText,
            SalaryMin = original.SalaryMin,
            SalaryMax = original.SalaryMax,
            Status = RequisitionStatus.Draft,
            OwnerId = currentUser.Id ?? string.Empty // Assign to the current user who cloned it
        };

        await repository.AddAsync(clone, ct);

        return Result.Success(clone.Id);
    }
}
