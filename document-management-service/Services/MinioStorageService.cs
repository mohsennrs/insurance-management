using Minio;
using Minio.DataModel.Args;

namespace document_management_service.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _bucketName = configuration["MinIO:BucketName"] ?? "claimflow-documents";
        _logger = logger;

        EnsureBucketExistsAsync().Wait();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName));

            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName));

                _logger.LogInformation("Created MinIO bucket: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists: {BucketName}", _bucketName);
            throw;
        }
    }

    public async Task<(string bucketName, string objectKey)> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var objectKey = $"{Guid.NewGuid()}/{fileName}";

        try
        {
            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType));

            _logger.LogInformation("Uploaded file to MinIO: {ObjectKey}", objectKey);

            return (_bucketName, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to MinIO: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string bucketName,
        string objectKey,
        int expirySeconds = 3600)
    {
        try
        {
            var url = await _minioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey)
                    .WithExpiry(expirySeconds));

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for: {ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectKey)
    {
        try
        {
            await _minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey));

            _logger.LogInformation("Deleted file from MinIO: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from MinIO: {ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectKey)
    {
        try
        {
            await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey));

            return true;
        }
        catch
        {
            return false;
        }
    }
}