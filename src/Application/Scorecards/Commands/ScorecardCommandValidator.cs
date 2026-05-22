using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Commands;

public class SubmitScorecardValidator : AbstractValidator<SubmitScorecard>
{
    private static readonly string[] RequiredDimensions =
        ["technical", "communication", "problemSolving", "cultureFit"];

    public SubmitScorecardValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Ratings)
            .NotEmpty();

        RuleFor(x => x.Ratings)
            .Must(r => RequiredDimensions.All(d => r.ContainsKey(d)))
            .WithMessage("Ratings must include: technical, communication, problemSolving, cultureFit.")
            .When(x => x.Ratings.Count > 0);

        RuleFor(x => x.Ratings)
            .Must(r => r.Values.All(v => v is >= 1 and <= 5))
            .WithMessage("All ratings must be between 1 and 5.")
            .When(x => x.Ratings.Count > 0);

        RuleFor(x => x.Recommendation)
            .NotEmpty()
            .Must(r => Enum.TryParse<ScorecardRecommendation>(r, out _))
            .WithMessage("Recommendation must be one of: StrongYes, Yes, No, StrongNo.");

        RuleFor(x => x.Strengths)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Concerns)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}
