using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Interviews.Queries;

// No Authorize attribute so candidates can potentially see it if they have a scheduling link
// Alternatively, protect and use a separate public endpoint
public record GetAvailability(string InterviewerId, DateTimeOffset From, DateTimeOffset To) : IRequest<Result<List<AvailabilityDto>>>;

public record AvailabilityDto(Guid Id, DateTimeOffset StartTime, DateTimeOffset EndTime);

public class GetAvailabilityHandler : IRequestHandler<GetAvailability, Result<List<AvailabilityDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAvailabilityHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AvailabilityDto>>> Handle(GetAvailability request, CancellationToken ct)
    {
        var availabilities = await _context.InterviewerAvailabilities
            .AsNoTracking()
            .Where(a => a.InterviewerId == request.InterviewerId && !a.IsBooked)
            .Where(a => a.StartTime >= request.From && a.EndTime <= request.To)
            .OrderBy(a => a.StartTime)
            .Select(a => new AvailabilityDto(a.Id, a.StartTime, a.EndTime))
            .ToListAsync(ct);

        return Result.Success(availabilities);
    }
}
