namespace RC.HyRe.Application.Requisitions.Commands;

public class CreateRequisitionValidator : AbstractValidator<CreateRequisition>
{
    public CreateRequisitionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JdText).NotEmpty();
        RuleFor(x => x.Headcount).GreaterThan(0);
        RuleFor(x => x.SalaryMin).GreaterThanOrEqualTo(0).When(x => x.SalaryMin.HasValue);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(0).When(x => x.SalaryMax.HasValue);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue);
    }
}

public class UpdateRequisitionValidator : AbstractValidator<UpdateRequisition>
{
    public UpdateRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JdText).NotEmpty();
        RuleFor(x => x.Headcount).GreaterThan(0);
    }
}

public class RejectRequisitionValidator : AbstractValidator<RejectRequisition>
{
    public RejectRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
