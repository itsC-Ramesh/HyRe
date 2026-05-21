using System.Linq.Expressions;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    string Enqueue(Expression<Func<Task>> methodCall);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
}
