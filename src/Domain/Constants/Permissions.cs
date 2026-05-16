namespace RC.HyRe.Domain.Constants;

public static class Permissions
{
    // Requisitions
    public const string RequisitionsCreate = "requisitions:create";
    public const string RequisitionsRead = "requisitions:read";
    public const string RequisitionsUpdate = "requisitions:update";
    public const string RequisitionsDelete = "requisitions:delete";

    // Candidates
    public const string CandidatesCreate = "candidates:create";
    public const string CandidatesRead = "candidates:read";
    public const string CandidatesUpdate = "candidates:update";
    public const string CandidatesDelete = "candidates:delete";

    // Pipeline
    public const string PipelineRead = "pipeline:read";
    public const string PipelineUpdate = "pipeline:update";

    // Scorecards
    public const string ScorecardsCreate = "scorecards:create";
    public const string ScorecardsRead = "scorecards:read";
    public const string ScorecardsUpdate = "scorecards:update";

    // Offers
    public const string OffersCreate = "offers:create";
    public const string OffersRead = "offers:read";
    public const string OffersUpdate = "offers:update";

    // Analytics
    public const string AnalyticsRead = "analytics:read";

    // Templates
    public const string TemplatesCreate = "templates:create";
    public const string TemplatesRead = "templates:read";
    public const string TemplatesUpdate = "templates:update";
    public const string TemplatesDelete = "templates:delete";

    // Users
    public const string UsersCreate = "users:create";
    public const string UsersRead = "users:read";
    public const string UsersUpdate = "users:update";
    public const string UsersDelete = "users:delete";

    // Comms
    public const string CommsRead = "comms:read";
    public const string CommsCreate = "comms:create";

    // Onboarding
    public const string OnboardingRead = "onboarding:read";
    public const string OnboardingUpdate = "onboarding:update";

    public static IReadOnlyList<string> GetPermissionsForRole(string role) => role switch
    {
        Roles.HrAdmin => new[]
        {
            RequisitionsCreate, RequisitionsRead, RequisitionsUpdate, RequisitionsDelete,
            CandidatesCreate, CandidatesRead, CandidatesUpdate, CandidatesDelete,
            PipelineRead, PipelineUpdate,
            ScorecardsRead,
            OffersCreate, OffersRead, OffersUpdate,
            AnalyticsRead,
            TemplatesCreate, TemplatesRead, TemplatesUpdate, TemplatesDelete,
            UsersCreate, UsersRead, UsersUpdate, UsersDelete,
            CommsRead, CommsCreate,
            OnboardingRead, OnboardingUpdate
        },
        Roles.HiringManager => new[]
        {
            RequisitionsCreate, RequisitionsRead, RequisitionsUpdate, RequisitionsDelete,
            CandidatesRead,
            PipelineRead, PipelineUpdate,
            ScorecardsRead,
            OffersRead,
            AnalyticsRead,
            CommsRead,
            OnboardingRead
        },
        Roles.Interviewer => new[]
        {
            CandidatesRead,
            ScorecardsCreate, ScorecardsRead, ScorecardsUpdate
        },
        Roles.Executive => new[]
        {
            RequisitionsRead,
            PipelineRead,
            AnalyticsRead
        },
        Roles.Candidate => new[]
        {
            CandidatesRead,
            OffersRead,
            CommsRead,
            OnboardingRead
        },
        Roles.Administrator => new[]
        {
            RequisitionsCreate, RequisitionsRead, RequisitionsUpdate, RequisitionsDelete,
            CandidatesCreate, CandidatesRead, CandidatesUpdate, CandidatesDelete,
            PipelineRead, PipelineUpdate,
            ScorecardsCreate, ScorecardsRead, ScorecardsUpdate,
            OffersCreate, OffersRead, OffersUpdate,
            AnalyticsRead,
            TemplatesCreate, TemplatesRead, TemplatesUpdate, TemplatesDelete,
            UsersCreate, UsersRead, UsersUpdate, UsersDelete,
            CommsRead, CommsCreate,
            OnboardingRead, OnboardingUpdate
        },
        _ => Array.Empty<string>()
    };
}
