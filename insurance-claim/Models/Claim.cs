namespace insurance_claim.Models;

public class Claim
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public ClaimType ClaimType { get; set; }
    public ClaimStatus Status { get; set; }
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
}

public enum ClaimType
{
    Auto = 1,
    Property = 2,
    Health = 3,
    Life = 4,
    Liability = 5,
    WorkersCompensation = 6,
    Travel = 7,
    Marine = 8
}

public enum ClaimStatus
{
    Submitted = 1,
    UnderReview = 2,
    InvestigationRequired = 3,
    Approved = 4,
    Rejected = 5,
    Settled = 6,
    Closed = 7
}