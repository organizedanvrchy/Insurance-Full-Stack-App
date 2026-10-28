using Insurance.Catalog.Contracts;
using Insurance.Catalog.Models;
using Insurance.Catalog.Repositories;

namespace Insurance.Catalog.Services;

/// <summary>Implements the module's public contract on top of its own repository.</summary>
public class PlanCatalog : IPlanCatalog
{
    private readonly IPolicyRepository _policies;
    public PlanCatalog(IPolicyRepository policies) => _policies = policies;

    public async Task<PlanInfo?> GetAsync(int policyId)
    {
        var p = await _policies.GetByIdAsync(policyId);
        return p is null ? null : ToInfo(p);
    }

    public async Task<IReadOnlyDictionary<int, PlanInfo>> GetManyAsync(IEnumerable<int> policyIds) =>
        (await _policies.GetManyAsync(policyIds)).ToDictionary(p => p.PolicyId, ToInfo);

    private static PlanInfo ToInfo(Policy p) =>
        new(p.PolicyId, p.PolicyName, p.PolicyType, p.CoverageAmount, p.MonthlyPremium, p.TermMonths, p.IsActive);
}
