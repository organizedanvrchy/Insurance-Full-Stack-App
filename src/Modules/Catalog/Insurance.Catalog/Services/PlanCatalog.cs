using Insurance.Catalog.Contracts;
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
        return p is null
            ? null
            : new PlanInfo(p.PolicyId, p.PolicyName, p.PolicyType, p.CoverageAmount, p.MonthlyPremium, p.TermMonths, p.IsActive);
    }
}
