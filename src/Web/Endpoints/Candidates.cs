using RC.HyRe.Application.Candidates.Commands;
using RC.HyRe.Application.Candidates.Queries;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Candidates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Create, "");
        groupBuilder.MapGet(GetAll, "");
        groupBuilder.MapGet(GetById, "{id}");
        groupBuilder.MapPut(Update, "{id}");
        groupBuilder.MapPost(Apply, "{id}/apply");
    }

    public static async Task<IResult> Create(
        ISender sender,
        CreateCandidate command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail(
                "CREATE_CANDIDATE_FAILED",
                "Failed to create candidate.",
                result.Errors));
    }

    public static async Task<IResult> GetAll(
        ISender sender,
        string? name,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetCandidates(name, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail(
                "GET_CANDIDATES_FAILED",
                "Failed to retrieve candidates.",
                result.Errors));
    }

    public static async Task<IResult> GetById(
        ISender sender,
        Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetCandidateById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail(
                "CANDIDATE_NOT_FOUND",
                "Candidate not found.",
                result.Errors));
    }

    public static async Task<IResult> Update(
        ISender sender,
        Guid id,
        UpdateCandidateBody body,
        CancellationToken ct)
    {
        var command = new UpdateCandidate(
            id, body.Name, body.Phone, body.Source, body.SourceDetail);

        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail(
                "UPDATE_CANDIDATE_FAILED",
                "Failed to update candidate.",
                result.Errors));
    }

    public static async Task<IResult> Apply(
        ISender sender,
        Guid id,
        ApplyBody body,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ApplyToRequisition(id, body.RequisitionId), ct);

        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail(
                "APPLY_FAILED",
                "Failed to apply candidate to requisition.",
                result.Errors));
    }
}

public record UpdateCandidateBody(
    string Name,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail);

public record ApplyBody(Guid RequisitionId);
