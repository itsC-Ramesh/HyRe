using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Requisitions.Commands;
using RC.HyRe.Application.Requisitions.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Requisitions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Create, "");
        groupBuilder.MapGet(GetAll, "");
        groupBuilder.MapGet(GetById, "{id}");
        groupBuilder.MapPut(Update, "{id}");
        groupBuilder.MapPost(Submit, "{id}/submit");
        groupBuilder.MapPost(Approve, "{id}/approve");
        groupBuilder.MapPost(Reject, "{id}/reject");
        groupBuilder.MapPost(Hold, "{id}/hold");
        groupBuilder.MapPost(Close, "{id}/close");
    }

    public static async Task<IResult> Create(ISender sender, CreateRequisition command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_REQUISITION_FAILED", "Failed to create requisition.", result.Errors));
    }

    public static async Task<IResult> GetAll(
        ISender sender,
        RequisitionStatus? status,
        string? department,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetRequisitions(status, department, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_REQUISITIONS_FAILED", "Failed to retrieve requisitions.", result.Errors));
    }

    public static async Task<IResult> GetById(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRequisitionById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("REQUISITION_NOT_FOUND", "Requisition not found.", result.Errors));
    }

    public static async Task<IResult> Update(ISender sender, Guid id, UpdateRequisitionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRequisition(id, body.Title, body.Department, body.JdText, body.SalaryMin, body.SalaryMax, body.Headcount), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("UPDATE_REQUISITION_FAILED", "Failed to update requisition.", result.Errors));
    }

    public static async Task<IResult> Submit(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SubmitForApproval(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("SUBMIT_FAILED", "Failed to submit requisition.", result.Errors));
    }

    public static async Task<IResult> Approve(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("APPROVE_FAILED", "Failed to approve requisition.", result.Errors));
    }

    public static async Task<IResult> Reject(ISender sender, Guid id, RejectRequisitionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RejectRequisition(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("REJECT_FAILED", "Failed to reject requisition.", result.Errors));
    }

    public static async Task<IResult> Hold(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new HoldRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("HOLD_FAILED", "Failed to hold requisition.", result.Errors));
    }

    public static async Task<IResult> Close(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CloseRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("CLOSE_FAILED", "Failed to close requisition.", result.Errors));
    }
}

public record UpdateRequisitionBody(string Title, string Department, string JdText, int? SalaryMin, int? SalaryMax, int Headcount);
public record RejectRequisitionBody(string Reason);
