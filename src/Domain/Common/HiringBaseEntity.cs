using System.ComponentModel.DataAnnotations.Schema;

namespace RC.HyRe.Domain.Common;

/// <summary>
/// Base class for all hiring-domain entities.
/// Uses Guid as the primary key (vs. the scaffold BaseEntity which uses int).
/// Inherits audit timestamps (Created, CreatedBy, LastModified, LastModifiedBy)
/// from BaseAuditableEntity — populated automatically by AuditableEntityInterceptor.
/// Domain events are supported via AddDomainEvent.
/// </summary>
public abstract class HiringBaseEntity : IHasDomainEvents, IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    private readonly List<BaseEvent> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void RemoveDomainEvent(BaseEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
