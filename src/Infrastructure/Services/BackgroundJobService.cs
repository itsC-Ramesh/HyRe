using System.Linq.Expressions;
using Hangfire;
using RC.HyRe.Application.Common.Interfaces;

namespace RC.HyRe.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    public void AddOrUpdateRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression);
    }
}
