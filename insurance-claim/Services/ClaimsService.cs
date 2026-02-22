using Microsoft.EntityFrameworkCore;
using insurance_claim.Data;
using insurance_claim.Models;
using shared_messaging;
using shared_messaging.Events;
namespace insurance_claim.Services;

public class ClaimsService : IClaimsService
{
    private readonly ClaimsDbContext _context;
    private readonly ILogger<ClaimsService> _logger;
    private readonly IEventBus? _eventBus;

    public ClaimsService(
        ClaimsDbContext context,
        ILogger<ClaimsService> logger,
        IEventBus? eventBus = null)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ClaimResponseDto?> GetByIdAsync(Guid id)
    {
        var claim = await _context.Claims.FindAsync(id);

        if (claim == null)
        {
            _logger.LogWarning("Claim with ID {ClaimId} not found", id);
            return null;
        }

        return MapToResponseDto(claim);
    }

    public async Task<PaginatedResult<ClaimResponseDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        ClaimStatus? status = null,
        string? policyNumber = null)
    {
        var query = _context.Claims.AsQueryable();

        // Apply filters
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(policyNumber))
        {
            query = query.Where(c => c.PolicyNumber.Contains(policyNumber));
        }

        var totalCount = await query.CountAsync();

        var claims = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<ClaimResponseDto>
        {
            Items = claims.Select(MapToResponseDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ClaimResponseDto> CreateAsync(CreateClaimDto createDto)
    {
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = GenerateClaimNumber(),
            PolicyNumber = createDto.PolicyNumber,
            ClaimType = createDto.ClaimType,
            Status = ClaimStatus.Submitted,
            ClaimAmount = createDto.ClaimAmount,
            IncidentDate = createDto.IncidentDate,
            ReportedDate = DateTime.UtcNow,
            ClaimantName = createDto.ClaimantName,
            ClaimantEmail = createDto.ClaimantEmail,
            ClaimantPhone = createDto.ClaimantPhone,
            Description = createDto.Description,
            Notes = createDto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new claim with ID {ClaimId} and number {ClaimNumber}",
            claim.Id, claim.ClaimNumber);

        // Publish event
        _logger.LogInformation("Eventbus is available: {EventBusAvailable}", _eventBus != null);
        if (_eventBus != null)
        {
            var @event = new ClaimCreatedEvent
            {
                ClaimId = claim.Id,
                ClaimNumber = claim.ClaimNumber,
                PolicyNumber = claim.PolicyNumber,
                ClaimType = claim.ClaimType.ToString(),
                ClaimAmount = claim.ClaimAmount,
                ClaimantName = claim.ClaimantName,
                ClaimantEmail = claim.ClaimantEmail
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("Published ClaimCreatedEvent for claim {ClaimId}", claim.Id);
        }

        return MapToResponseDto(claim);
    }

    public async Task<ClaimResponseDto?> UpdateAsync(Guid id, UpdateClaimDto updateDto)
    {
        var claim = await _context.Claims.FindAsync(id);

        if (claim == null)
        {
            _logger.LogWarning("Claim with ID {ClaimId} not found for update", id);
            return null;
        }

        var oldStatus = claim.Status;
        var statusChanged = false;

        // Update only provided fields
        if (updateDto.Status.HasValue && updateDto.Status.Value != oldStatus)
        {
            claim.Status = updateDto.Status.Value;
            statusChanged = true;
        }

        if (updateDto.ClaimAmount.HasValue)
        {
            claim.ClaimAmount = updateDto.ClaimAmount.Value;
        }

        if (updateDto.Description != null)
        {
            claim.Description = updateDto.Description;
        }

        if (updateDto.AssignedTo != null)
        {
            claim.AssignedTo = updateDto.AssignedTo;
        }

        if (updateDto.Notes != null)
        {
            claim.Notes = updateDto.Notes;
        }

        claim.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated claim with ID {ClaimId}", id);

        // Publish status changed event
        if (_eventBus != null && statusChanged)
        {
            var @event = new ClaimStatusChangedEvent
            {
                ClaimId = claim.Id,
                ClaimNumber = claim.ClaimNumber,
                OldStatus = oldStatus.ToString(),
                NewStatus = claim.Status.ToString(),
                ChangedBy = claim.AssignedTo
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogInformation(
                "Published ClaimStatusChangedEvent for claim {ClaimId}: {OldStatus} → {NewStatus}",
                claim.Id, oldStatus, claim.Status);
        }

        return MapToResponseDto(claim);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var claim = await _context.Claims.FindAsync(id);

        if (claim == null)
        {
            _logger.LogWarning("Claim with ID {ClaimId} not found for deletion", id);
            return false;
        }

        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted claim with ID {ClaimId}", id);

        return true;
    }

    private static ClaimResponseDto MapToResponseDto(Claim claim)
    {
        return new ClaimResponseDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            PolicyNumber = claim.PolicyNumber,
            ClaimType = claim.ClaimType.ToString(),
            Status = claim.Status.ToString(),
            ClaimAmount = claim.ClaimAmount,
            IncidentDate = claim.IncidentDate,
            ReportedDate = claim.ReportedDate,
            ClaimantName = claim.ClaimantName,
            ClaimantEmail = claim.ClaimantEmail,
            ClaimantPhone = claim.ClaimantPhone,
            Description = claim.Description,
            AssignedTo = claim.AssignedTo,
            CreatedAt = claim.CreatedAt,
            UpdatedAt = claim.UpdatedAt,
            Notes = claim.Notes
        };
    }

    private static string GenerateClaimNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"CLM-{timestamp}-{random}";
    }
}