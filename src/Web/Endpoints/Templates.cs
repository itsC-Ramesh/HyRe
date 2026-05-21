using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Templates.Queries;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Templates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTemplates, "");
        groupBuilder.MapGet(GetTemplate, "{id}");
        groupBuilder.MapPost(CreateTemplate, "")
            .RequireAuthorization();
        groupBuilder.MapPut(UpdateTemplate, "{id}")
            .RequireAuthorization();
        groupBuilder.MapDelete(DeleteTemplate, "{id}")
            .RequireAuthorization();
        groupBuilder.MapPost(RenderPreview, "{id}/preview");
        groupBuilder.MapPost(RenderByCategory, "render");
    }

    public static async Task<IResult> GetTemplates(
        ITemplateService templateService,
        TemplateCategory? category,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await templateService.GetPagedAsync(category, page, limit, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_TEMPLATES_FAILED", "Failed to retrieve templates.", result.Errors));
    }

    public static async Task<IResult> GetTemplate(
        ITemplateService templateService,
        Guid id,
        CancellationToken ct = default)
    {
        var result = await templateService.GetByIdAsync(id, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("TEMPLATE_NOT_FOUND", "Template not found.", result.Errors));
    }

    public static async Task<IResult> CreateTemplate(
        ITemplateService templateService,
        IUser user,
        CreateTemplateRequest request,
        CancellationToken ct = default)
    {
        if (!IsTemplateManager(user))
            return TypedResults.Forbid();

        var result = await templateService.CreateAsync(
            request.Name,
            request.Category,
            request.Subject,
            request.Body,
            ct);

        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_TEMPLATE_FAILED", "Failed to create template.", result.Errors));
    }

    public static async Task<IResult> UpdateTemplate(
        ITemplateService templateService,
        IUser user,
        Guid id,
        UpdateTemplateRequest request,
        CancellationToken ct = default)
    {
        if (!IsTemplateManager(user))
            return TypedResults.Forbid();

        var result = await templateService.UpdateAsync(id, request.Subject, request.Body, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("UPDATE_TEMPLATE_FAILED", "Failed to update template.", result.Errors));
    }

    public static async Task<IResult> DeleteTemplate(
        ITemplateService templateService,
        IUser user,
        Guid id,
        CancellationToken ct = default)
    {
        if (!IsTemplateManager(user))
            return TypedResults.Forbid();

        var result = await templateService.DeleteAsync(id, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("DELETE_TEMPLATE_FAILED", "Failed to delete template.", result.Errors));
    }

    public static async Task<IResult> RenderPreview(
        ITemplateService templateService,
        Guid id,
        CancellationToken ct = default)
    {
        var result = await templateService.PreviewAsync(id, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("PREVIEW_FAILED", "Failed to preview template.", result.Errors));
    }

    public static async Task<IResult> RenderByCategory(
        ITemplateService templateService,
        RenderRequest request,
        CancellationToken ct = default)
    {
        var result = await templateService.RenderByCategoryAsync(request.Category, request.Variables, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("RENDER_FAILED", "Failed to render template.", result.Errors));
    }

    private static bool IsTemplateManager(IUser user)
    {
        return user.Roles is not null
            && (user.Roles.Contains(Roles.HrAdmin) || user.Roles.Contains(Roles.Administrator));
    }
}

public record CreateTemplateRequest(string Name, TemplateCategory Category, string Subject, string Body);
public record UpdateTemplateRequest(string Subject, string Body);
public record RenderRequest(TemplateCategory Category, Dictionary<string, string> Variables);
