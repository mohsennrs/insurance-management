using Microsoft.AspNetCore.Mvc;
using document_management_service.Models;
using document_management_service.Services;

namespace document_management_service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentsService _documentsService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentsService documentsService,
        ILogger<DocumentsController> logger)
    {
        _documentsService = documentsService;
        _logger = logger;
    }

    /// <summary>Upload a document for a claim</summary>
    /// <param name="claimId">The claim GUID</param>
    /// <param name="file">The file to upload</param>
    /// <param name="documentType">Type of document (0=Unknown, 1=Invoice, 2=MedicalReport, 3=PoliceReport, 4=Photo, etc.)</param>
    /// <param name="description">Optional description</param>
    /// <param name="tags">Optional comma-separated tags</param>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(52428800)] // 50MB limit
    public async Task<ActionResult<DocumentResponseDto>> UploadDocument(
        [FromForm] Guid claimId,
        [FromForm] IFormFile file,
        [FromForm] DocumentType documentType = DocumentType.Unknown,
        [FromForm] string? description = null,
        [FromForm] string? tags = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required and cannot be empty." });

        if (file.Length > 52428800) // 50MB
            return BadRequest(new { message = "File size cannot exceed 50MB." });

        try
        {
            var document = await _documentsService.UploadDocumentAsync(
                claimId,
                file,
                documentType,
                description,
                tags);

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = document.Id },
                document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for claim {ClaimId}", claimId);
            return StatusCode(500, new { message = "Failed to upload document.", error = ex.Message });
        }
    }

    /// <summary>Get a document by ID</summary>
    /// <param name="id">Document GUID</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponseDto>> GetDocument(Guid id)
    {
        var document = await _documentsService.GetDocumentByIdAsync(id);

        if (document == null)
            return NotFound(new { message = $"Document with ID {id} not found." });

        return Ok(document);
    }

    /// <summary>Get all documents for a specific claim</summary>
    /// <param name="claimId">Claim GUID</param>
    [HttpGet("claim/{claimId:guid}")]
    [ProducesResponseType<DocumentListResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentListResponseDto>> GetDocumentsByClaim(Guid claimId)
    {
        var documents = await _documentsService.GetDocumentsByClaimIdAsync(claimId);
        return Ok(documents);
    }

    /// <summary>Get a presigned download URL for a document (valid for 1 hour)</summary>
    /// <param name="id">Document GUID</param>
    /// <param name="expirySeconds">URL expiry in seconds (default: 3600 = 1 hour)</param>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType<DownloadUrlResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DownloadUrlResponseDto>> GetDownloadUrl(
        Guid id,
        [FromQuery] int expirySeconds = 3600)
    {
        if (expirySeconds < 60 || expirySeconds > 604800) // 1 min to 7 days
            return BadRequest(new { message = "expirySeconds must be between 60 and 604800." });

        var result = await _documentsService.GetDownloadUrlAsync(id, expirySeconds);

        if (result == null)
            return NotFound(new { message = $"Document with ID {id} not found." });

        return Ok(result);
    }

    /// <summary>Delete a document permanently</summary>
    /// <param name="id">Document GUID</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var deleted = await _documentsService.DeleteDocumentAsync(id);

        if (!deleted)
            return NotFound(new { message = $"Document with ID {id} not found." });

        return NoContent();
    }

    /// <summary>Health check</summary>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() =>
        Ok(new { status = "healthy", service = "documents", timestamp = DateTime.UtcNow });
}