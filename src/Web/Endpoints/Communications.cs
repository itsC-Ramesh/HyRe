using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Communications.Queries;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Web.Endpoints;

public class Communications : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetFeed, "feed/{entityId}").RequireAuthorization(policy => policy.RequireRole(Roles.HrAdmin, Roles.HiringManager, Roles.Interviewer));
    }

    public static async Task<IResult> GetFeed(ISender sender, Guid entityId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCommunicationsFeed(entityId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_FEED_FAILED", "Failed to retrieve communications feed.", result.Errors));
    }
}
