namespace RC.HyRe.Application.Interviews.Commands;

public class ScheduleInterviewValidator : AbstractValidator<ScheduleInterview>
{
    public ScheduleInterviewValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.InterviewerId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
        RuleFor(x => x.DurationMin).InclusiveBetween(15, 480);
    }
}

public class RescheduleInterviewValidator : AbstractValidator<RescheduleInterview>
{
    public RescheduleInterviewValidator()
    {
        RuleFor(x => x.InterviewId).NotEmpty();
        RuleFor(x => x.NewScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}
