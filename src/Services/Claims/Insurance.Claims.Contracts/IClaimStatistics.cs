using Insurance.BuildingBlocks.Http;

namespace Insurance.Claims.Contracts;

/// <summary>Claim counts for the admin customer list (Claims' internal API, used by the gateway's BFF).</summary>
public interface IClaimStatistics
{
    // Customers with no claims are missing from the result.
    Task<IReadOnlyDictionary<int, ClaimCounts>> GetCountsByUserAsync(IEnumerable<int> userIds);
}

/// <summary>Open = Submitted or UnderReview.</summary>
public record ClaimCounts(int TotalClaims, int OpenClaims);

/// <summary>Calls /internal/claims on the Claims service.</summary>
public class ClaimStatisticsClient : IClaimStatistics
{
    private readonly HttpClient _http;
    public ClaimStatisticsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyDictionary<int, ClaimCounts>> GetCountsByUserAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ClaimCounts>();

        var response = await _http.GetAsync($"internal/claims/counts?{ServiceClients.IdQuery("userIds", ids)}");
        return await response.ReadOrThrowAsync<Dictionary<int, ClaimCounts>>();
    }
}
