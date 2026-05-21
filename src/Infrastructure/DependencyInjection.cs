using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Infrastructure.Data;
using RC.HyRe.Infrastructure.Data.Interceptors;
using RC.HyRe.Infrastructure.Data.Repositories;
using RC.HyRe.Infrastructure.Identity;
using RC.HyRe.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("JwtSettings");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("SecretKey missing"))),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        var isBuildingOpenApi = Environment.GetCommandLineArgs()
            .Any(arg => arg.Contains("getdocument") || arg.Contains("swagger") || arg.Contains("openapi"));

        if (!isBuildingOpenApi)
        {
            builder.Services.AddHangfireServer();
        }

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        // File storage
        var fileStorageProvider = builder.Configuration.GetSection("FileStorage")["Provider"] ?? "Local";
        if (fileStorageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddSingleton<IFileStorageService, S3StorageService>();
        else
            builder.Services.AddSingleton<IFileStorageService, LocalStorageService>();

        builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
        builder.Services.AddScoped<IRequisitionRepository, RequisitionRepository>();
        builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
        builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
        builder.Services.AddScoped<IScorecardRepository, ScorecardRepository>();
        builder.Services.AddScoped<IOfferRepository, OfferRepository>();

        builder.Services.AddScoped<ITemplateService, TemplateService>();
        builder.Services.AddScoped<NotificationJobService>();
    }
}
