using RC.HyRe.Infrastructure.Data;
using RC.HyRe.Infrastructure.Services;
using Scalar.AspNetCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
var isBuildingOpenApi = Environment.GetCommandLineArgs()
    .Any(arg => arg.Contains("getdocument") || arg.Contains("swagger") || arg.Contains("openapi"));

if (app.Environment.IsDevelopment() && !isBuildingOpenApi)
{
    try
    {
        await app.InitialiseDatabaseAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization skipped/failed: {ex.Message}");
    }
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(builder =>
{
    var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["https://localhost:4200"];
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .WithOrigins(allowedOrigins)
        .AllowCredentials();
});

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();

if (!isBuildingOpenApi)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [ new RC.HyRe.Web.Infrastructure.HangfireDashboardAuthorizationFilter() ]
    });

    // Register recurring notification jobs
    RecurringJob.AddOrUpdate<NotificationJobService>(
        "interview-reminders",
        service => service.SendInterviewRemindersAsync(CancellationToken.None),
        "*/15 * * * *");

    RecurringJob.AddOrUpdate<NotificationJobService>(
        "overdue-scorecards",
        service => service.SendOverdueScorecardRemindersAsync(CancellationToken.None),
        "0 9 * * *");

    RecurringJob.AddOrUpdate<NotificationJobService>(
        "stale-approvals",
        service => service.EscalateStaleApprovalsAsync(CancellationToken.None),
        "0 9 * * *");

    RecurringJob.AddOrUpdate<NotificationJobService>(
        "retry-failed-notifications",
        service => service.RetryFailedNotificationsAsync(CancellationToken.None),
        "*/30 * * * *");
}

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

app.MapFallbackToFile("index.html");

app.Run();
