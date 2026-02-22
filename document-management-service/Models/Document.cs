namespace document_management_service.Models;

public class Document
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DocumentType DocumentType { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

public enum DocumentType
{
    Unknown = 0,
    Invoice = 1,
    MedicalReport = 2,
    PoliceReport = 3,
    Photo = 4,
    Correspondence = 5,
    LegalDocument = 6,
    Other = 99
}
