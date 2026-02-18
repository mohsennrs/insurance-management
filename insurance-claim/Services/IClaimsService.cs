using insurance_claim.Models;

namespace insurance_claim.Services;

public interface IClaimsService
{
    Task<ClaimResponseDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<ClaimResponseDto>> GetAllAsync(
        int pageNumber = 1, 
        int pageSize = 10,
        ClaimStatus? status = null,
        string? policyNumber = null);
    Task<ClaimResponseDto> CreateAsync(CreateClaimDto createDto);
    Task<ClaimResponseDto?> UpdateAsync(Guid id, UpdateClaimDto updateDto);
    Task<bool> DeleteAsync(Guid id);
}