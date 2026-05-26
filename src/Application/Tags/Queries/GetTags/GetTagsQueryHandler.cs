using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Tags.Queries.GetTags;

public class GetTagsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTagsQuery, Result<List<TagDto>>>
{
    public async Task<Result<List<TagDto>>> Handle(GetTagsQuery request, CancellationToken ct)
    {
        var tags = await db.Tags.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);

        return Result.Success(tags);
    }
}
