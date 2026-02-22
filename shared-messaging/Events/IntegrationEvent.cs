namespace shared_messaging.Events;

/// <summary>
/// Base class for all events
/// </summary>
public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published by Claims Service when a new claim is created
/// </summary>
public class ClaimCreatedEvent : IntegrationEvent
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantEmail { get; set; } = string.Empty;
}

/// <summary>
/// Published by Claims Service when a claim status changes
/// </summary>
public class ClaimStatusChangedEvent : IntegrationEvent
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
}

/// <summary>
/// Published by Claims Service when a claim is created with files
/// Documents Service will handle storing the files
/// </summary>
public class StoreDocumentRequestEvent : IntegrationEvent
{
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public int DocumentType { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Published by Documents Service when a document is uploaded
/// </summary>
public class DocumentUploadedEvent : IntegrationEvent
{
    public Guid DocumentId { get; set; }
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

/// <summary>
/// Published by Documents Service when a document is deleted
/// </summary>
public class DocumentDeletedEvent : IntegrationEvent
{
    public Guid DocumentId { get; set; }
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
}