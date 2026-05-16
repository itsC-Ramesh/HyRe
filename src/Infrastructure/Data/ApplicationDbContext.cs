using System.Reflection;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RC.HyRe.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ── Legacy scaffold ────────────────────────────────────────────────
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    // ── Auth / audit ───────────────────────────────────────────────────
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Hiring domain ──────────────────────────────────────────────────
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<Scorecard> Scorecards => Set<Scorecard>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
