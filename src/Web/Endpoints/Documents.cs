using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Documents.Commands;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Documents : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Upload, "upload").RequireAuthorization().DisableAntiforgery();
    }

    public static async Task<IResult> Upload(
        ISender sender, 
        [FromForm] IFormFile file, 
        [FromForm] string entityType, 
        [FromForm] Guid entityId, 
        [FromForm] DocumentType documentType, 
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return TypedResults.BadRequest(ApiResponse.Fail("UPLOAD_FAILED", "File is empty."));

        await using var stream = file.OpenReadStream();

        var command = new UploadDocument(
            stream,
            file.FileName,
            file.ContentType,
            entityType,
            entityId,
            documentType
        );

        var result = await sender.Send(command, ct);

        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("UPLOAD_FAILED", "Failed to upload document.", result.Errors));
    }
}
