using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<Result<StoredFile>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Result<string>> GetPresignedUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default);
    Task<Result> DeleteAsync(string fileKey, CancellationToken ct = default);
}

public record StoredFile(string FileKey, long SizeBytes);
