using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Web.Infrastructure;

namespace RC.HyRe.Web.Endpoints;

public class Files : IEndpointGroup
{
    static string? IEndpointGroup.RoutePrefix => "/api/files";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(UploadResume, "upload/resume/{candidateId}")
            .RequireAuthorization()
            .DisableAntiforgery();
        groupBuilder.MapPost(UploadOfferLetter, "upload/offer-letter/{offerId}")
            .RequireAuthorization()
            .DisableAntiforgery();
        groupBuilder.MapGet(Download, "download/{fileKey}")
            .RequireAuthorization();
        groupBuilder.MapGet(GetPresignedUrl, "presigned/{fileKey}")
            .RequireAuthorization();
        groupBuilder.MapDelete(DeleteFile, "{fileKey}")
            .RequireAuthorization();
    }

    public static async Task<IResult> UploadResume(
        Guid candidateId,
        IFormFile file,
        IFileStorageService fileStorageService,
        IApplicationDbContext dbContext,
        IUser user,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.Id))
            return TypedResults.Unauthorized();

        if (!HasPermission(user, Permissions.CandidatesUpdate))
            return TypedResults.Forbid();

        var validationResult = ValidateFile(file, configuration);
        if (validationResult is not null)
            return validationResult;

        var candidate = await dbContext.Candidates.FindAsync([candidateId], ct);
        if (candidate is null)
            return TypedResults.NotFound(ApiResponse.Fail("CANDIDATE_NOT_FOUND", "Candidate not found."));

        var uploadResult = await fileStorageService.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct);
        if (!uploadResult.Succeeded)
            return TypedResults.BadRequest(ApiResponse.Fail("UPLOAD_FAILED", "File upload failed.", uploadResult.Errors));

        var document = new Document
        {
            EntityType = "candidate",
            EntityId = candidateId,
            FileKey = uploadResult.Value.FileKey,
            Type = DocumentType.Resume,
            MimeType = file.ContentType,
            SizeBytes = uploadResult.Value.SizeBytes
        };

        dbContext.Documents.Add(document);
        candidate.ResumeDocId = document.Id;
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(ApiResponse.Ok(new { documentId = document.Id, fileKey = document.FileKey }));
    }

    public static async Task<IResult> UploadOfferLetter(
        Guid offerId,
        IFormFile file,
        IFileStorageService fileStorageService,
        IApplicationDbContext dbContext,
        IUser user,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.Id))
            return TypedResults.Unauthorized();

        if (!HasPermission(user, Permissions.OffersUpdate))
            return TypedResults.Forbid();

        var validationResult = ValidateFile(file, configuration);
        if (validationResult is not null)
            return validationResult;

        var offer = await dbContext.Offers.FindAsync([offerId], ct);
        if (offer is null)
            return TypedResults.NotFound(ApiResponse.Fail("OFFER_NOT_FOUND", "Offer not found."));

        var uploadResult = await fileStorageService.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct);
        if (!uploadResult.Succeeded)
            return TypedResults.BadRequest(ApiResponse.Fail("UPLOAD_FAILED", "File upload failed.", uploadResult.Errors));

        var document = new Document
        {
            EntityType = "offer",
            EntityId = offerId,
            FileKey = uploadResult.Value.FileKey,
            Type = DocumentType.OfferLetter,
            MimeType = file.ContentType,
            SizeBytes = uploadResult.Value.SizeBytes
        };

        dbContext.Documents.Add(document);
        offer.LetterDocId = document.Id;
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(ApiResponse.Ok(new { documentId = document.Id, fileKey = document.FileKey }));
    }

    public static async Task<IResult> Download(
        string fileKey,
        IFileStorageService fileStorageService,
        IApplicationDbContext dbContext,
        IUser user,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.Id))
            return TypedResults.Unauthorized();

        var document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.FileKey == fileKey, ct);

        if (document is null)
            return TypedResults.NotFound(ApiResponse.Fail("FILE_NOT_FOUND", "File not found."));

        if (!HasPermission(user, new[] { Roles.HrAdmin, Roles.HiringManager }))
        {
            if (document.EntityType == "candidate")
            {
                var candidate = await dbContext.Candidates.FindAsync([document.EntityId], ct);
                if (candidate is null || candidate.Email != user.Email)
                    return TypedResults.Forbid();
            }
            else
            {
                return TypedResults.Forbid();
            }
        }

        var urlResult = await fileStorageService.GetPresignedUrlAsync(fileKey, TimeSpan.FromHours(1), ct);
        if (!urlResult.Succeeded)
            return TypedResults.BadRequest(ApiResponse.Fail("PRESIGN_FAILED", "Failed to generate download URL.", urlResult.Errors));

        // Local storage: the URL result is the file key itself — serve the file directly
        var basePath = configuration.GetSection("FileStorage")["BasePath"] ?? "./uploads";
        var fullPath = Path.Combine(basePath, fileKey);

        if (!File.Exists(fullPath))
            return TypedResults.NotFound(ApiResponse.Fail("FILE_NOT_FOUND", "File not found on disk."));

        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        return TypedResults.File(bytes, document.MimeType, Path.GetFileName(fileKey));
    }

    public static async Task<IResult> GetPresignedUrl(
        string fileKey,
        IFileStorageService fileStorageService,
        IApplicationDbContext dbContext,
        IUser user,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.Id))
            return TypedResults.Unauthorized();

        var document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.FileKey == fileKey, ct);

        if (document is null)
            return TypedResults.NotFound(ApiResponse.Fail("FILE_NOT_FOUND", "File not found."));

        if (!HasPermission(user, new[] { Roles.HrAdmin, Roles.HiringManager }))
        {
            if (document.EntityType == "candidate")
            {
                var candidate = await dbContext.Candidates.FindAsync([document.EntityId], ct);
                if (candidate is null || candidate.Email != user.Email)
                    return TypedResults.Forbid();
            }
            else
            {
                return TypedResults.Forbid();
            }
        }

        var urlResult = await fileStorageService.GetPresignedUrlAsync(fileKey, TimeSpan.FromHours(1), ct);
        if (!urlResult.Succeeded)
            return TypedResults.BadRequest(ApiResponse.Fail("PRESIGN_FAILED", "Failed to generate presigned URL.", urlResult.Errors));

        return TypedResults.Ok(ApiResponse.Ok(new { url = urlResult.Value }));
    }

    public static async Task<IResult> DeleteFile(
        string fileKey,
        IFileStorageService fileStorageService,
        IApplicationDbContext dbContext,
        IUser user,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.Id))
            return TypedResults.Unauthorized();

        if (!HasPermission(user, new[] { Roles.HrAdmin }))
            return TypedResults.Forbid();

        var document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.FileKey == fileKey, ct);

        if (document is null)
            return TypedResults.NotFound(ApiResponse.Fail("FILE_NOT_FOUND", "File not found."));

        // Nullify FK on owning entity
        if (document.EntityType == "candidate")
        {
            var candidate = await dbContext.Candidates.FindAsync([document.EntityId], ct);
            if (candidate is not null)
                candidate.ResumeDocId = null;
        }
        else if (document.EntityType == "offer")
        {
            var offer = await dbContext.Offers.FindAsync([document.EntityId], ct);
            if (offer is not null)
                offer.LetterDocId = null;
        }

        // Remove document record
        dbContext.Documents.Remove(document);
        await fileStorageService.DeleteAsync(fileKey, ct);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(ApiResponse.Ok());
    }

    private static IResult? ValidateFile(IFormFile file, IConfiguration configuration)
    {
        if (file is null || file.Length == 0)
            return TypedResults.BadRequest(ApiResponse.Fail("NO_FILE", "No file was provided."));

        var maxFileSize = configuration.GetSection("FileStorage").GetValue<long>("MaxFileSizeBytes");
        if (maxFileSize == 0) maxFileSize = 10485760; // 10MB default

        if (file.Length > maxFileSize)
            return TypedResults.BadRequest(ApiResponse.Fail("FILE_TOO_LARGE", $"File size exceeds the maximum allowed size of {maxFileSize} bytes."));

        var allowedContentTypes = configuration.GetSection("FileStorage:AllowedContentTypes").Get<string[]>()
            ?? ["application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"];

        if (!allowedContentTypes.Contains(file.ContentType))
            return TypedResults.BadRequest(ApiResponse.Fail("INVALID_CONTENT_TYPE", $"Content type '{file.ContentType}' is not allowed."));

        return null;
    }

    private static bool HasPermission(IUser user, string permission)
    {
        if (user.Roles is null) return false;

        foreach (var role in user.Roles)
        {
            var permissions = Permissions.GetPermissionsForRole(role);
            if (permissions.Contains(permission))
                return true;
        }

        return false;
    }

    private static bool HasPermission(IUser user, string[] roles)
    {
        if (user.Roles is null) return false;
        return user.Roles.Any(r => roles.Contains(r));
    }
}
