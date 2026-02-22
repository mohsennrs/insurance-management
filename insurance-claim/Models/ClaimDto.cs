namespace insurance_claim.Models;

public class CreateClaimDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public ClaimType ClaimType { get; set; }
    public decimal ClaimAmount { get; set; }
    public DateTime IncidentDate { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantEmail { get; set; } = string.Empty;
    public string? ClaimantPhone { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateClaimWithFilesDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public int ClaimType { get; set; }
    public decimal ClaimAmount { get; set; }
    public DateTime IncidentDate { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantEmail { get; set; } = string.Empty;
    public string? ClaimantPhone { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdateClaimDto
{
    public ClaimStatus? Status { get; set; }
    public decimal? ClaimAmount { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

public class ClaimResponseDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedDate { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantEmail { get; set; } = string.Empty;
    public string? ClaimantPhone { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public List<string>? AttachedDocuments { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}