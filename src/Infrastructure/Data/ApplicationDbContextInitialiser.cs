using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RC.HyRe.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // Production-safe: always migrate.
            // To wipe the DB in local dev only, set  Database:ForceRecreate=true
            // in appsettings.Development.json (never in production).
            var forceRecreate = _configuration.GetValue<bool>("Database:ForceRecreate");
            if (forceRecreate)
            {
                _logger.LogWarning("Database:ForceRecreate is true — dropping and recreating the database.");
                await _context.Database.EnsureDeletedAsync();
            }

            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // ── 1. Roles ──────────────────────────────────────────────────────────────
        var roles = new[]
        {
            Roles.Administrator, Roles.HrAdmin, Roles.HiringManager,
            Roles.Interviewer, Roles.Executive, Roles.Candidate
        };

        foreach (var roleName in roles)
        {
            if (_roleManager.Roles.All(r => r.Name != roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // ── 2. Seed users ─────────────────────────────────────────────────────────
        var administrator = new ApplicationUser
        {
            UserName = "administrator@localhost",
            Email = "administrator@localhost",
            FullName = "System Administrator",
        };
        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            await _userManager.AddToRolesAsync(administrator, [Roles.Administrator, Roles.HrAdmin]);
        }

        var hiringManager = new ApplicationUser
        {
            UserName = "manager@hyreapp.local",
            Email = "manager@hyreapp.local",
            FullName = "Alex Manager",
            Department = "Engineering",
        };
        if (_userManager.Users.All(u => u.UserName != hiringManager.UserName))
        {
            await _userManager.CreateAsync(hiringManager, "Manager1!");
            await _userManager.AddToRoleAsync(hiringManager, Roles.HiringManager);
        }

        var interviewer = new ApplicationUser
        {
            UserName = "interviewer@hyreapp.local",
            Email = "interviewer@hyreapp.local",
            FullName = "Jordan Interviewer",
            Department = "Engineering",
        };
        if (_userManager.Users.All(u => u.UserName != interviewer.UserName))
        {
            await _userManager.CreateAsync(interviewer, "Interviewer1!");
            await _userManager.AddToRoleAsync(interviewer, Roles.Interviewer);
        }

        // ── 3. Hiring domain — skip if already seeded ─────────────────────────────
        if (await _context.Candidates.AnyAsync())
        {
            _logger.LogInformation("Hiring domain data already seeded — skipping.");
            return;
        }

        var hmUser = await _userManager.FindByEmailAsync(hiringManager.Email) ?? hiringManager;

        // ── 4. Requisitions ───────────────────────────────────────────────────────
        var reqEngineering = new Requisition
        {
            Title = "Senior Software Engineer",
            Department = "Engineering",
            OwnerId = hmUser.Id,
            JdText = "We are looking for a senior engineer to join our platform team.",
            SalaryMin = 2_000_000,
            SalaryMax = 3_000_000,
            Status = RequisitionStatus.Open,
        };
        var reqProduct = new Requisition
        {
            Title = "Product Manager",
            Department = "Product",
            OwnerId = hmUser.Id,
            JdText = "Drive the roadmap for our unified hiring platform.",
            SalaryMin = 1_800_000,
            SalaryMax = 2_500_000,
            Status = RequisitionStatus.Open,
        };

        _context.Requisitions.AddRange(reqEngineering, reqProduct);
        await _context.SaveChangesAsync(CancellationToken.None);

        // ── 5. Candidates ─────────────────────────────────────────────────────────
        var candidates = new Candidate[]
        {
            new() { Name = "Priya Sharma",  Email = "priya.sharma@example.com",  Source = CandidateSource.LinkedIn },
            new() { Name = "Arjun Mehta",   Email = "arjun.mehta@example.com",   Source = CandidateSource.Referral, SourceDetail = "Alex Manager" },
            new() { Name = "Sofia Chen",    Email = "sofia.chen@example.com",    Source = CandidateSource.JobBoard, SourceDetail = "Naukri" },
            new() { Name = "Daniel Okafor", Email = "daniel.okafor@example.com", Source = CandidateSource.Direct },
            new() { Name = "Mei Lin",       Email = "mei.lin@example.com",       Source = CandidateSource.Agency,   SourceDetail = "TalentBridge" },
        };

        _context.Candidates.AddRange(candidates);
        await _context.SaveChangesAsync(CancellationToken.None);

        // ── 6. Applications — one per candidate, spread across pipeline stages ────
        var stages = new[]
        {
            ApplicationStage.Applied,
            ApplicationStage.Screened,
            ApplicationStage.Interview,
            ApplicationStage.Offer,
            ApplicationStage.Hired,
        };

        for (var i = 0; i < candidates.Length; i++)
        {
            _context.Applications.Add(new JobApplication
            {
                CandidateId    = candidates[i].Id,
                RequisitionId  = (i < 3) ? reqEngineering.Id : reqProduct.Id,
                Stage          = stages[i],
            });
        }

        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Hiring domain seed data inserted successfully.");
    }
}
