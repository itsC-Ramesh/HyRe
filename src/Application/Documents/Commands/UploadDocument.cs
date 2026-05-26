using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Documents.Commands;

[Authorize(Roles = $"{Roles.Candidate},{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record UploadDocument(
    Stream FileStream,
    string FileName,
    string ContentType,
    string EntityType,
    Guid EntityId,
    DocumentType DocumentType
) : IRequest<Result<Guid>>;

public class UploadDocumentHandler : IRequestHandler<UploadDocument, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _storageService;

    public UploadDocumentHandler(IApplicationDbContext context, IFileStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<Result<Guid>> Handle(UploadDocument request, CancellationToken ct)
    {
        // 1. Upload file to S3
        var uploadResult = await _storageService.UploadAsync(request.FileStream, request.FileName, request.ContentType, ct);
        if (!uploadResult.Succeeded)
        {
            return Result.Failure<Guid>("Failed to upload document to storage.");
        }

        var storedFile = uploadResult.Value;

        // 2. Create Document record in DB
        var document = new Document
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            FileKey = storedFile.FileKey,
            Type = request.DocumentType,
            MimeType = request.ContentType,
            SizeBytes = storedFile.SizeBytes
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);

        return Result.Success(document.Id);
    }
}
