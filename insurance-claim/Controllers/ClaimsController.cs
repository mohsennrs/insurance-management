using Microsoft.AspNetCore.Mvc;
using insurance_claim.Models;
using insurance_claim.Services;

namespace insurance_claim.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IClaimsService claimsService, ILogger<ClaimsController> logger)
    {
        _claimsService = claimsService;
        _logger = logger;
    }

    /// <summary>
    /// Get a claim by ID
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>Claim details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponseDto>> GetClaim(Guid id)
    {
        var claim = await _claimsService.GetByIdAsync(id);

        if (claim == null)
        {
            return NotFound(new { message = $"Claim with ID {id} not found" });
        }

        return Ok(claim);
    }

    /// <summary>
    /// Get all claims with optional filtering and pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="status">Filter by claim status</param>
    /// <param name="policyNumber">Filter by policy number</param>
    /// <returns>Paginated list of claims</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ClaimResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ClaimResponseDto>>> GetAllClaims(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ClaimStatus? status = null,
        [FromQuery] string? policyNumber = null)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Invalid pagination parameters" });
        }

        var result = await _claimsService.GetAllAsync(pageNumber, pageSize, status, policyNumber);
        return Ok(result);
    }

    /// <summary>
    /// Create a new claim
    /// </summary>
    /// <param name="createDto">Claim creation data</param>
    /// <returns>Created claim</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponseDto>> CreateClaim([FromBody] CreateClaimDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var claim = await _claimsService.CreateAsync(createDto);

        return CreatedAtAction(
            nameof(GetClaim),
            new { id = claim.Id },
            claim);
    }


    /// <summary>Create a new claim with file attachments (sends files via RabbitMQ to Documents Service)</summary>
    /// <param name="policyNumber">Insurance policy number</param>
    /// <param name="claimType">Type of claim (1=Auto, 2=Property, 3=Health, 4=Life, 5=Liability, 6=WorkersCompensation, 7=Travel, 8=Marine)</param>
    /// <param name="claimAmount">Claim amount</param>
    /// <param name="incidentDate">Date of incident (ISO 8601 format)</param>
    /// <param name="claimantName">Name of claimant</param>
    /// <param name="claimantEmail">Email of claimant</param>
    /// <param name="claimantPhone">Phone number (optional)</param>
    /// <param name="description">Description of the claim</param>
    /// <param name="notes">Additional notes (optional)</param>
    /// <param name="attachments">File attachments (optional, max 100MB total)</param>
    /// <returns>Created claim</returns>
    [HttpPost("with-files")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ClaimResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(104857600)] // 100MB total
    public async Task<ActionResult<ClaimResponseDto>> CreateClaimWithFiles(
        [FromForm] string policyNumber,
        [FromForm] int claimType,
        [FromForm] decimal claimAmount,
        [FromForm] DateTime incidentDate,
        [FromForm] string claimantName,
        [FromForm] string claimantEmail,
        [FromForm] string? claimantPhone,
        [FromForm] string description,
        [FromForm] string? notes,
        [FromForm] List<IFormFile>? attachments)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate claim type
        if (!Enum.IsDefined(typeof(ClaimType), claimType))
            return BadRequest(new { message = "Invalid claim type. Must be 1-8." });

        var createDto = new CreateClaimDto
        {
            PolicyNumber = policyNumber,
            ClaimType = (ClaimType)claimType,
            ClaimAmount = claimAmount,
            IncidentDate = incidentDate,
            ClaimantName = claimantName,
            ClaimantEmail = claimantEmail,
            ClaimantPhone = claimantPhone,
            Description = description,
            Notes = notes
        };

        var claim = await _claimsService.CreateAsync(createDto);

        // If files are attached, publish events to Documents Service
        if (attachments != null && attachments.Any())
        {
            var documentNames = new List<string>();
            var eventBus = HttpContext.RequestServices.GetService<shared_messaging.Events.IEventBus>();

            if (eventBus == null)
            {
                _logger.LogWarning("Event bus not configured. Cannot send documents via events.");
                return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, claim);
            }

            foreach (var file in attachments)
            {
                if (file.Length > 52428800) // 50MB per file
                {
                    _logger.LogWarning("Skipping file {FileName} - exceeds 50MB limit", file.FileName);
                    continue;
                }

                try
                {
                    // Read file into memory
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileData = memoryStream.ToArray();

                    // Publish event to Documents Service
                    var storeDocumentEvent = new shared_messaging.Events.StoreDocumentRequestEvent
                    {
                        ClaimId = claim.Id,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileData = fileData,
                        DocumentType = 0, // Unknown type
                        Description = $"Auto-uploaded with claim {claim.ClaimNumber}",
                        Tags = "claim-upload,auto"
                    };

                    await eventBus.PublishAsync(storeDocumentEvent);

                    documentNames.Add(file.FileName);
                    _logger.LogInformation(
                        "Published StoreDocumentRequestEvent for {FileName} ({Size} bytes) to claim {ClaimId}",
                        file.FileName,
                        fileData.Length,
                        claim.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing document event for {FileName}", file.FileName);
                }
            }

            if (documentNames.Any())
            {
                claim.AttachedDocuments = documentNames;
            }
        }

        return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, claim);
    }
    /// <summary>
    /// Update an existing claim
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <param name="updateDto">Claim update data</param>
    /// <returns>Updated claim</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponseDto>> UpdateClaim(
        Guid id,
        [FromBody] UpdateClaimDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var claim = await _claimsService.UpdateAsync(id, updateDto);

        if (claim == null)
        {
            return NotFound(new { message = $"Claim with ID {id} not found" });
        }

        return Ok(claim);
    }

    /// <summary>
    /// Delete a claim
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClaim(Guid id)
    {
        var deleted = await _claimsService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new { message = $"Claim with ID {id} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}