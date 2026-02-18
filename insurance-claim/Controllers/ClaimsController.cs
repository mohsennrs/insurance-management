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