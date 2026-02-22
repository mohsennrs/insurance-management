namespace document_management_service.Services;

public interface IStorageService
{
    Task<(string bucketName, string objectKey)> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType);

    Task<string> GetPresignedDownloadUrlAsync(
        string bucketName,
        string objectKey,
        int expirySeconds = 3600);

    Task DeleteFileAsync(string bucketName, string objectKey);

    Task<bool> FileExistsAsync(string bucketName, string objectKey);
}