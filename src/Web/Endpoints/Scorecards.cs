using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Scorecards.Commands;
using RC.HyRe.Application.Scorecards.Queries;

namespace RC.HyRe.Web.Endpoints;

public class Scorecards : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetByInterview, "interview/{interviewId}").RequireAuthorization();
        groupBuilder.MapGet(GetMy, "my").RequireAuthorization();
        groupBuilder.MapGet(GetByApplication, "application/{applicationId}").RequireAuthorization();
        groupBuilder.MapPost(Submit, "{id}/submit").RequireAuthorization();
    }

    public static async Task<IResult> GetByInterview(
        ISender sender, Guid interviewId, CancellationToken ct)
    {
        var result = await sender.Send(new GetScorecardByInterview(interviewId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("SCORECARD_NOT_FOUND", "Scorecard not found.", result.Errors));
    }

    public static async Task<IResult> GetMy(
        ISender sender, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetScorecardsByInterviewer(page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_SCORECARDS_FAILED", "Failed to retrieve scorecards.", result.Errors));
    }

    public static async Task<IResult> GetByApplication(
        ISender sender, Guid applicationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetScorecardsByApplication(applicationId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_SCORECARDS_FAILED", "Failed to retrieve scorecards.", result.Errors));
    }

    public static async Task<IResult> Submit(
        ISender sender, Guid id, SubmitScorecardBody body, CancellationToken ct)
    {
        var command = new SubmitScorecard(
            id, body.Ratings, body.Recommendation,
            body.Strengths, body.Concerns, body.Notes);

        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("SUBMIT_FAILED", "Failed to submit scorecard.", result.Errors));
    }
}

public record SubmitScorecardBody(
    Dictionary<string, int> Ratings,
    string Recommendation,
    string Strengths,
    string Concerns,
    string? Notes
);
