using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Tags.Commands.CreateTag;

public class CreateTagCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateTagCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTagCommand request, CancellationToken ct)
    {
        if (await db.Tags.AnyAsync(t => t.Name.ToLower() == request.Name.ToLower(), ct))
            return Result<Guid>.Failure("A tag with this name already exists.");

        var tag = new Tag
        {
            Name = request.Name,
            Color = request.Color
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);

        return Result.Success(tag.Id);
    }
}
