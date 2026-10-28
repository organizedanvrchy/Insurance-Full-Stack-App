using Insurance.Claims.Contracts;
using Insurance.Claims.Repositories;

namespace Insurance.Claims.Services;

/// <summary>Implements the service's internal API on top of its own repository.</summary>
public class ClaimStatistics : IClaimStatistics
{
    private readonly IClaimRepository _claims;
    public ClaimStatistics(IClaimRepository claims) => _claims = claims;

    public async Task<IReadOnlyDictionary<int, ClaimCounts>> GetCountsByUserAsync(IEnumerable<int> userIds) =>
        await _claims.GetCountsByUserAsync(userIds);
}
