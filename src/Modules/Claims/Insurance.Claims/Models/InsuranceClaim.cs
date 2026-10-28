using Insurance.Catalog.Contracts;
using Insurance.Claims.Enums;

namespace Insurance.Claims.Models;

// Named InsuranceClaim (not Claim) so it can't clash with System.Security.Claims.Claim used by the JWT code.
public class InsuranceClaim
{
    public int ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }                       // the customer, for ownership checks
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public DateOnly IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public ClaimStatus Status { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
}