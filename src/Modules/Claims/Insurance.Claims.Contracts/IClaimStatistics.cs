namespace Insurance.Claims.Contracts;

/// <summary>Claim counts for the admin customer list.</summary>
public interface IClaimStatistics
{
    // Customers with no claims are missing from the result.
    Task<IReadOnlyDictionary<int, ClaimCounts>> GetCountsByUserAsync(IEnumerable<int> userIds);
}

/// <summary>Open = Submitted or UnderReview.</summary>
public record ClaimCounts(int TotalClaims, int OpenClaims);
