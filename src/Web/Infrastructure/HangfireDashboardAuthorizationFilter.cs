using Hangfire.Dashboard;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Web.Infrastructure;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        return user.Identity is { IsAuthenticated: true } && user.IsInRole(Roles.HrAdmin);
    }
}
