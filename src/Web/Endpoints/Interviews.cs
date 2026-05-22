using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Interviews.Commands;
using RC.HyRe.Application.Interviews.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Interviews : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Schedule, "").RequireAuthorization();
        groupBuilder.MapGet(GetByApplication, "application/{applicationId}").RequireAuthorization();
        groupBuilder.MapGet(GetMy, "my").RequireAuthorization();
        groupBuilder.MapPut(Reschedule, "{id}/reschedule").RequireAuthorization();
        groupBuilder.MapPost(Cancel, "{id}/cancel").RequireAuthorization();
        groupBuilder.MapPost(NoShow, "{id}/no-show").RequireAuthorization();
        groupBuilder.MapPost(Complete, "{id}/complete").RequireAuthorization();
    }

    public static async Task<IResult> Schedule(ISender sender, ScheduleInterview command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("SCHEDULE_FAILED", "Failed to schedule interview.", result.Errors));
    }

    public static async Task<IResult> GetByApplication(
        ISender sender, Guid applicationId, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetInterviewsByApplication(applicationId, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_INTERVIEWS_FAILED", "Failed to retrieve interviews.", result.Errors));
    }

    public static async Task<IResult> GetMy(
        ISender sender, InterviewStatus? status, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetInterviewsByInterviewer(status, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_INTERVIEWS_FAILED", "Failed to retrieve interviews.", result.Errors));
    }

    public static async Task<IResult> Reschedule(ISender sender, Guid id, RescheduleBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RescheduleInterview(id, body.NewScheduledAt), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("RESCHEDULE_FAILED", "Failed to reschedule interview.", result.Errors));
    }

    public static async Task<IResult> Cancel(ISender sender, Guid id, CancelBody body, CancellationToken ct)
    {
        var result = await sender.Send(new CancelInterview(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("CANCEL_FAILED", "Failed to cancel interview.", result.Errors));
    }

    public static async Task<IResult> NoShow(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNoShow(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("NO_SHOW_FAILED", "Failed to mark no-show.", result.Errors));
    }

    public static async Task<IResult> Complete(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkCompleted(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("COMPLETE_FAILED", "Failed to mark completed.", result.Errors));
    }
}

public record RescheduleBody(DateTimeOffset NewScheduledAt);
public record CancelBody(string? Reason);
