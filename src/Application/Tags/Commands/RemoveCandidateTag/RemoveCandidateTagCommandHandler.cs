using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Tags.Commands.RemoveCandidateTag;

public class RemoveCandidateTagCommandHandler(IApplicationDbContext db) : IRequestHandler<RemoveCandidateTagCommand, Result>
{
    public async Task<Result> Handle(RemoveCandidateTagCommand request, CancellationToken ct)
    {
        var candidateTag = await db.CandidateTags
            .FirstOrDefaultAsync(ct => ct.CandidateId == request.CandidateId && ct.TagId == request.TagId, ct);

        if (candidateTag != null)
        {
            db.CandidateTags.Remove(candidateTag);
            await db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
