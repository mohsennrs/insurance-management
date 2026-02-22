using shared_messaging;
using shared_messaging.Events;
using document_management_service.Data;
using document_management_service.Models;
using document_management_service.Services;

namespace document_management_service.EventHandlers;

/// <summary>
/// Handles StoreDocumentRequestEvent from Claims Service
/// Receives file data via event and stores it in MinIO + database
/// </summary>
public class StoreDocumentRequestEventHandler : IEventHandler<StoreDocumentRequestEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StoreDocumentRequestEventHandler> _logger;

    public StoreDocumentRequestEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<StoreDocumentRequestEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(StoreDocumentRequestEvent @event)
    {
        _logger.LogInformation(
            "📥 Received StoreDocumentRequestEvent: {FileName} ({Size} bytes) for claim {ClaimId}",
            @event.FileName,
            @event.FileData.Length,
            @event.ClaimId);

        using var scope = _scopeFactory.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        var eventBus = scope.ServiceProvider.GetService<IEventBus>();

        try
        {
            // Upload file to MinIO
            using var fileStream = new MemoryStream(@event.FileData);
            var (bucketName, objectKey) = await storageService.UploadFileAsync(
                fileStream,
                @event.FileName,
                @event.ContentType);

            _logger.LogInformation(
                "✅ Uploaded {FileName} to MinIO: {BucketName}/{ObjectKey}",
                @event.FileName,
                bucketName,
                objectKey);

            // Save metadata to database
            var document = new Document
            {
                Id = Guid.NewGuid(),
                ClaimId = @event.ClaimId,
                FileName = @event.FileName,
                ContentType = @event.ContentType,
                FileSizeBytes = @event.FileData.Length,
                DocumentType = (DocumentType)@event.DocumentType,
                BucketName = bucketName,
                ObjectKey = objectKey,
                StorageUrl = $"minio://{bucketName}/{objectKey}",
                UploadedAt = DateTime.UtcNow,
                Description = @event.Description,
                Tags = @event.Tags
            };

            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Saved document metadata to database: DocumentId={DocumentId}",
                document.Id);

            // Publish DocumentUploadedEvent back to Claims Service
            if (eventBus != null)
            {
                var uploadedEvent = new DocumentUploadedEvent
                {
                    DocumentId = document.Id,
                    ClaimId = document.ClaimId,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    FileSizeBytes = document.FileSizeBytes,
                    DocumentType = document.DocumentType.ToString()
                };

                await eventBus.PublishAsync(uploadedEvent);

                _logger.LogInformation(
                    "📤 Published DocumentUploadedEvent for document {DocumentId}",
                    document.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error storing document {FileName} for claim {ClaimId}",
                @event.FileName,
                @event.ClaimId);
            throw; // Re-throw to trigger RabbitMQ retry
        }
    }
}