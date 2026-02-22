namespace document_management_service.Models;

public class UploadDocumentDto
{
    public Guid ClaimId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

public class DocumentResponseDto
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeMB { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

public class DocumentListResponseDto
{
    public List<DocumentResponseDto> Documents { get; set; } = new();
    public int TotalCount { get; set; }
    public Guid ClaimId { get; set; }
}

public class DownloadUrlResponseDto
{
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
}