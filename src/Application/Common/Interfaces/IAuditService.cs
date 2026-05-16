namespace RC.HyRe.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null,
                  object? metadata = null, CancellationToken ct = default);
}
