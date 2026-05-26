using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

public class InterviewerAvailability : HiringBaseEntity
{
    public required string InterviewerId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsBooked { get; set; }
}
