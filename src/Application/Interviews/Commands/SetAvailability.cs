using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Roles = $"{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record SetAvailability(
    List<AvailabilitySlotDto> Slots
) : IRequest<Result>;

public record AvailabilitySlotDto(DateTimeOffset StartTime, DateTimeOffset EndTime);

public class SetAvailabilityHandler : IRequestHandler<SetAvailability, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetAvailabilityHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(SetAvailability request, CancellationToken ct)
    {
        // For simplicity, we replace all unbooked future availability for this user.
        var now = DateTimeOffset.UtcNow;
        var existingUnbooked = await _context.InterviewerAvailabilities
            .Where(a => a.InterviewerId == _user.Id && !a.IsBooked && a.StartTime >= now)
            .ToListAsync(ct);

        _context.InterviewerAvailabilities.RemoveRange(existingUnbooked);

        var newAvailabilities = request.Slots.Select(s => new InterviewerAvailability
        {
            InterviewerId = _user.Id!,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            IsBooked = false
        });

        _context.InterviewerAvailabilities.AddRange(newAvailabilities);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
