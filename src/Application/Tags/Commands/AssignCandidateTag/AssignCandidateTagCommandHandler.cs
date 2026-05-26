using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Tags.Commands.AssignCandidateTag;

public class AssignCandidateTagCommandHandler(IApplicationDbContext db, IUser currentUser) : IRequestHandler<AssignCandidateTagCommand, Result>
{
    public async Task<Result> Handle(AssignCandidateTagCommand request, CancellationToken ct)
    {
        var candidateExists = await db.Candidates.AnyAsync(c => c.Id == request.CandidateId, ct);
        if (!candidateExists)
            return Result.Failure("Candidate not found.");

        var tagExists = await db.Tags.AnyAsync(t => t.Id == request.TagId, ct);
        if (!tagExists)
            return Result.Failure("Tag not found.");

        var alreadyAssigned = await db.CandidateTags.AnyAsync(ct => ct.CandidateId == request.CandidateId && ct.TagId == request.TagId, ct);
        if (alreadyAssigned)
            return Result.Success(); // Idempotent

        db.CandidateTags.Add(new CandidateTag
        {
            CandidateId = request.CandidateId,
            TagId = request.TagId,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = currentUser.Id
        });

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
