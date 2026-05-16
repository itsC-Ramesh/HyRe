namespace RC.HyRe.Domain.Constants;

public abstract class Roles
{
    public const string Administrator = nameof(Administrator); // keep for superadmin/seeding
    public const string HrAdmin = "hr_admin";
    public const string HiringManager = "hiring_manager";
    public const string Interviewer = "interviewer";
    public const string Executive = "executive";
    public const string Candidate = "candidate";

    // Convenience groups
    public const string InternalStaff = $"{HrAdmin},{HiringManager},{Interviewer},{Executive}";
    public const string AllRoles = $"{InternalStaff},{Candidate}";
}