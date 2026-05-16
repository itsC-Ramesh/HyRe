using System.Text.Json;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace RC.HyRe.Infrastructure.Identity;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public AuditService(
        IApplicationDbContext context, 
        IUser user, 
        IHttpContextAccessor httpContextAccessor, 
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null, object? metadata = null, CancellationToken ct = default)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var entry = new AuditLogEntry
        {
            Action = action,
            EntityType = entityType ?? "system",
            EntityId = entityId,
            ActorId = _user.Id,
            ActorRole = _user.Roles != null && _user.Roles.Any() ? string.Join(",", _user.Roles) : null,
            IpAddress = ipAddress,
            Metadata = metadata != null ? JsonSerializer.SerializeToDocument(metadata) : null,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        // We rely on the MediatR pipeline's unit of work to commit this, avoiding partial/premature commits.
        _context.AuditLogEntries.Add(entry);
    }
}
