using MediatR;
using RC.HyRe.Application.Tags.Commands.AssignCandidateTag;
using RC.HyRe.Application.Tags.Commands.CreateTag;
using RC.HyRe.Application.Tags.Commands.DeleteTag;
using RC.HyRe.Application.Tags.Commands.RemoveCandidateTag;
using RC.HyRe.Application.Tags.Queries.GetTags;
using RC.HyRe.Web.Infrastructure;

namespace RC.HyRe.Web.Endpoints;

public class TagsEndpoints : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/tags";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization()
            .WithTags("Tags");

        groupBuilder.MapGet("", GetTags).WithName("GetTags");
        groupBuilder.MapPost("", CreateTag).WithName("CreateTag");
        groupBuilder.MapDelete("{id:guid}", DeleteTag).WithName("DeleteTag");

        var candidatesGroup = groupBuilder.MapGroup("candidates/{candidateId:guid}");
        candidatesGroup.MapPost("{tagId:guid}", AssignCandidateTag).WithName("AssignCandidateTag");
        candidatesGroup.MapDelete("{tagId:guid}", RemoveCandidateTag).WithName("RemoveCandidateTag");
    }

    private static async Task<IResult> GetTags(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTagsQuery(), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_TAGS_FAILED", "Failed to get tags", result.Errors));
    }

    private static async Task<IResult> CreateTag(CreateTagCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Created($"/api/v1/tags/{result.Value}", ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_TAG_FAILED", "Failed to create tag", result.Errors));
    }

    private static async Task<IResult> DeleteTag(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTagCommand(id), ct);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("DELETE_TAG_FAILED", "Failed to delete tag", result.Errors));
    }

    private static async Task<IResult> AssignCandidateTag(Guid candidateId, Guid tagId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new AssignCandidateTagCommand(candidateId, tagId), ct);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("ASSIGN_TAG_FAILED", "Failed to assign tag", result.Errors));
    }

    private static async Task<IResult> RemoveCandidateTag(Guid candidateId, Guid tagId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveCandidateTagCommand(candidateId, tagId), ct);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("REMOVE_TAG_FAILED", "Failed to remove tag", result.Errors));
    }
}
