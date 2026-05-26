using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // ── Legacy scaffold entities ──────────────────────────────────
    DbSet<TodoList> TodoLists { get; }
    DbSet<TodoItem> TodoItems { get; }

    // ── Auth / audit ──────────────────────────────────────────────
    DbSet<AuditLogEntry> AuditLogEntries { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // ── Hiring domain ─────────────────────────────────────────────
    DbSet<Candidate> Candidates { get; }
    DbSet<Requisition> Requisitions { get; }
    DbSet<JobApplication> Applications { get; }
    DbSet<Interview> Interviews { get; }
    DbSet<Scorecard> Scorecards { get; }
    DbSet<Offer> Offers { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Document> Documents { get; }
    DbSet<Template> Templates { get; }
    DbSet<InterviewerAvailability> InterviewerAvailabilities { get; }
    DbSet<TemplateVersion> TemplateVersions { get; }
    DbSet<EventLog> EventLogs { get; }
    DbSet<Note> Notes { get; }
    DbSet<Tag> Tags { get; }
    DbSet<CandidateTag> CandidateTags { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
