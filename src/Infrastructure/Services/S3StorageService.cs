using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Infrastructure.Services;

/// <summary>
/// S3-compatible storage implementation.
/// Requires the AWSSDK.S3 NuGet package and proper configuration.
/// Set FileStorage:Provider to 'Local' for development.
/// </summary>
public class S3StorageService : IFileStorageService
{
    public Task<Result<StoredFile>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        throw new NotImplementedException("S3 storage not yet configured. Set FileStorage:Provider to 'Local' for development.");
    }

    public Task<Result<string>> GetPresignedUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default)
    {
        throw new NotImplementedException("S3 storage not yet configured. Set FileStorage:Provider to 'Local' for development.");
    }

    public Task<Result> DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        throw new NotImplementedException("S3 storage not yet configured. Set FileStorage:Provider to 'Local' for development.");
    }
}
