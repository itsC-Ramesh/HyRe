using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["S3:BucketName"] ?? "hyre-documents";
        _logger = logger;
    }

    public async Task<Result<StoredFile>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            var key = $"{Guid.NewGuid()}-{fileName}";
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request, ct);

            var storedFile = new StoredFile(key, fileStream.Length);
            return Result.Success(storedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to S3", fileName);
            return Result.Failure<StoredFile>("Failed to upload file.");
        }
    }

    public async Task<Result<string>> GetPresignedUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.Add(expiry)
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            return Result.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for {FileKey}", fileKey);
            return Result.Failure<string>("Failed to generate URL.");
        }
    }

    public async Task<Result> DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(request, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileKey} from S3", fileKey);
            return Result.Failure("Failed to delete file.");
        }
    }
}
