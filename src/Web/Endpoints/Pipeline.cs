using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Pipeline.Commands;
using RC.HyRe.Application.Pipeline.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Pipeline : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetByRequisition, "requisition/{requisitionId}");
        groupBuilder.MapGet(GetApplication, "applications/{id}");
        groupBuilder.MapPost(Advance, "applications/{id}/advance");
        groupBuilder.MapPost(Reject, "applications/{id}/reject");
        groupBuilder.MapPost(BulkAdvance, "applications/bulk-advance");
    }

    public static async Task<IResult> GetByRequisition(ISender sender, Guid requisitionId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPipelineByRequisition(requisitionId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("PIPELINE_NOT_FOUND", "Pipeline not found.", result.Errors));
    }

    public static async Task<IResult> GetApplication(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetApplicationById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("APPLICATION_NOT_FOUND", "Application not found.", result.Errors));
    }

    public static async Task<IResult> Advance(ISender sender, Guid id, AdvanceBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AdvanceApplicationStage(id, body.NewStage), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("ADVANCE_FAILED", "Failed to advance stage.", result.Errors));
    }

    public static async Task<IResult> Reject(ISender sender, Guid id, PipelineRejectBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RejectApplication(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("REJECT_FAILED", "Failed to reject application.", result.Errors));
    }

    public static async Task<IResult> BulkAdvance(ISender sender, BulkAdvanceBody body, CancellationToken ct)
    {
        var result = await sender.Send(new BulkAdvanceStage(body.ApplicationIds, body.NewStage), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("BULK_ADVANCE_FAILED", "Failed to advance applications.", result.Errors));
    }
}

public record AdvanceBody(ApplicationStage NewStage);
public record PipelineRejectBody(string? Reason);
public record BulkAdvanceBody(List<Guid> ApplicationIds, ApplicationStage NewStage);
