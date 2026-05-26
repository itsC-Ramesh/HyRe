using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Tags.Commands.DeleteTag;

public class DeleteTagCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteTagCommand, Result>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await db.Tags.FindAsync(new object[] { request.TagId }, ct);

        if (tag == null)
            return Result.Failure("Tag not found.");

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
