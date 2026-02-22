using document_management_service.Models;

namespace document_management_service.Services;

public interface IDocumentsService
{
    Task<DocumentResponseDto> UploadDocumentAsync(
        Guid claimId,
        IFormFile file,
        DocumentType documentType,
        string? description,
        string? tags);

    Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid id);

    Task<DocumentListResponseDto> GetDocumentsByClaimIdAsync(Guid claimId);

    Task<DownloadUrlResponseDto?> GetDownloadUrlAsync(Guid id, int expirySeconds = 3600);

    Task<bool> DeleteDocumentAsync(Guid id);
}