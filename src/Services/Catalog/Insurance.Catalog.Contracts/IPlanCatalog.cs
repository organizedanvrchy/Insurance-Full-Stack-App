using Insurance.BuildingBlocks.Http;

namespace Insurance.Catalog.Contracts;

/// <summary>Read access to the plan catalog for other services (Catalog's internal API).</summary>
public interface IPlanCatalog
{
    Task<PlanInfo?> GetAsync(int policyId);   // null => no such plan

    // Active or not: existing quotes, enrollments and claims still show retired plans.
    Task<IReadOnlyDictionary<int, PlanInfo>> GetManyAsync(IEnumerable<int> policyIds);
}

/// <summary>What other services need to know about a plan. MonthlyPremium is the base rate before rating.</summary>
public record PlanInfo(
    int PolicyId,
    string PolicyName,
    PolicyType PolicyType,
    decimal CoverageAmount,
    decimal MonthlyPremium,
    int TermMonths,
    bool IsActive);

/// <summary>Calls GET /internal/plans on the Catalog service.</summary>
public class PlanCatalogClient : IPlanCatalog
{
    private readonly HttpClient _http;
    public PlanCatalogClient(HttpClient http) => _http = http;

    public async Task<PlanInfo?> GetAsync(int policyId) =>
        (await GetManyAsync(new[] { policyId })).GetValueOrDefault(policyId);

    public async Task<IReadOnlyDictionary<int, PlanInfo>> GetManyAsync(IEnumerable<int> policyIds)
    {
        var ids = policyIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, PlanInfo>();

        var response = await _http.GetAsync($"internal/plans?{ServiceClients.IdQuery("ids", ids)}");
        var plans = await response.ReadOrThrowAsync<List<PlanInfo>>();
        return plans.ToDictionary(p => p.PolicyId);
    }
}
