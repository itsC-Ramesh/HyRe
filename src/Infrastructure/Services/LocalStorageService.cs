using Microsoft.Extensions.Configuration;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Infrastructure.Services;

public class LocalStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IConfiguration configuration)
    {
        _basePath = configuration.GetSection("FileStorage")["BasePath"] ?? "./uploads";
    }

    public Task<Result<StoredFile>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            var extension = Path.GetExtension(fileName);
            var fileKey = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(_basePath, fileKey);

            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            using var fileStream2 = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            fileStream.CopyTo(fileStream2);

            var storedFile = new StoredFile(fileKey, fileStream.Length);
            return Task.FromResult(Result.Success(storedFile));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<StoredFile>($"File upload failed: {ex.Message}"));
        }
    }

    public Task<Result<string>> GetPresignedUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default)
    {
        // For local storage, return the file key itself; the download endpoint will serve the file.
        return Task.FromResult(Result.Success(fileKey));
    }

    public Task<Result> DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, fileKey);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"File deletion failed: {ex.Message}"));
        }
    }
}
