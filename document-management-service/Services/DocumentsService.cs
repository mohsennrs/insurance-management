using Microsoft.EntityFrameworkCore;
using document_management_service.Data;
using document_management_service.Models;
using shared_messaging;
using shared_messaging.Events;

namespace document_management_service.Services;

public class DocumentsService : IDocumentsService
{
    private readonly DocumentsDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<DocumentsService> _logger;
    private readonly IEventBus? _eventBus;

    public DocumentsService(
        DocumentsDbContext context,
        IStorageService storageService,
        ILogger<DocumentsService> logger,
        IEventBus? eventBus = null)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<DocumentResponseDto> UploadDocumentAsync(
        Guid claimId,
        IFormFile file,
        DocumentType documentType,
        string? description,
        string? tags)
    {
        // Validate file
        if (file.Length == 0)
            throw new ArgumentException("File is empty");

        // Upload to storage
        using var stream = file.OpenReadStream();
        var (bucketName, objectKey) = await _storageService.UploadFileAsync(
            stream,
            file.FileName,
            file.ContentType);

        // Create database record
        var document = new Document
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            DocumentType = documentType,
            BucketName = bucketName,
            ObjectKey = objectKey,
            StorageUrl = $"minio://{bucketName}/{objectKey}",
            UploadedAt = DateTime.UtcNow,
            Description = description,
            Tags = tags
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Document uploaded: {DocumentId} for claim {ClaimId}",
            document.Id,
            claimId);

        // Publish event
        if (_eventBus != null)
        {
            var @event = new DocumentUploadedEvent
            {
                DocumentId = document.Id,
                ClaimId = document.ClaimId,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                DocumentType = document.DocumentType.ToString()
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("Published DocumentUploadedEvent for document {DocumentId}", document.Id);
        }

        return MapToResponseDto(document);
    }

    public async Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        return document == null ? null : MapToResponseDto(document);
    }

    public async Task<DocumentListResponseDto> GetDocumentsByClaimIdAsync(Guid claimId)
    {
        var documents = await _context.Documents
            .Where(d => d.ClaimId == claimId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return new DocumentListResponseDto
        {
            ClaimId = claimId,
            Documents = documents.Select(MapToResponseDto).ToList(),
            TotalCount = documents.Count
        };
    }

    public async Task<DownloadUrlResponseDto?> GetDownloadUrlAsync(Guid id, int expirySeconds = 3600)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return null;

        var url = await _storageService.GetPresignedDownloadUrlAsync(
            document.BucketName,
            document.ObjectKey,
            expirySeconds);

        return new DownloadUrlResponseDto
        {
            DownloadUrl = url,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expirySeconds),
            FileName = document.FileName
        };
    }

    public async Task<bool> DeleteDocumentAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return false;

        // Delete from storage
        await _storageService.DeleteFileAsync(document.BucketName, document.ObjectKey);

        // Delete from database
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document deleted: {DocumentId}", id);

        // Publish event
        if (_eventBus != null)
        {
            var @event = new DocumentDeletedEvent
            {
                DocumentId = document.Id,
                ClaimId = document.ClaimId,
                FileName = document.FileName
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("Published DocumentDeletedEvent for document {DocumentId}", id);
        }

        return true;
    }

    private static DocumentResponseDto MapToResponseDto(Document document)
    {
        return new DocumentResponseDto
        {
            Id = document.Id,
            ClaimId = document.ClaimId,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            FileSizeMB = $"{document.FileSizeBytes / (1024.0 * 1024.0):F2} MB",
            DocumentType = document.DocumentType.ToString(),
            StorageUrl = document.StorageUrl,
            UploadedAt = document.UploadedAt,
            Description = document.Description,
            Tags = document.Tags
        };
    }
}
