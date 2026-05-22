using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

public class AdvanceApplicationStageValidator : AbstractValidator<AdvanceApplicationStage>
{
    public AdvanceApplicationStageValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.NewStage).IsInEnum();
        RuleFor(x => x.NewStage).NotEqual(ApplicationStage.Rejected)
            .WithMessage("Use the Reject command to reject an application.");
    }
}

public class RejectApplicationValidator : AbstractValidator<RejectApplication>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}

public class BulkAdvanceStageValidator : AbstractValidator<BulkAdvanceStage>
{
    public BulkAdvanceStageValidator()
    {
        RuleFor(x => x.ApplicationIds).NotEmpty();
        RuleFor(x => x.NewStage).IsInEnum();
        RuleFor(x => x.NewStage).NotEqual(ApplicationStage.Rejected);
    }
}
