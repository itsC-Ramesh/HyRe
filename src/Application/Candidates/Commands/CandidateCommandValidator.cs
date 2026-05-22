namespace RC.HyRe.Application.Candidates.Commands;

public class CreateCandidateValidator : AbstractValidator<CreateCandidate>
{
    public CreateCandidateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Phone)
            .MaximumLength(20);
    }
}

public class UpdateCandidateValidator : AbstractValidator<UpdateCandidate>
{
    public UpdateCandidateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Candidate ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(20);
    }
}

public class ApplyToRequisitionValidator : AbstractValidator<ApplyToRequisition>
{
    public ApplyToRequisitionValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty().WithMessage("Candidate ID is required.");

        RuleFor(x => x.RequisitionId)
            .NotEmpty().WithMessage("Requisition ID is required.");
    }
}
