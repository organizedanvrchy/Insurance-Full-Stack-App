namespace Insurance.Catalog.Contracts;

/// <summary>Read access to the plan catalog for other modules.</summary>
public interface IPlanCatalog
{
    Task<PlanInfo?> GetAsync(int policyId);   // null => no such plan

    // Active or not: existing quotes, enrollments and claims still show retired plans.
    Task<IReadOnlyDictionary<int, PlanInfo>> GetManyAsync(IEnumerable<int> policyIds);
}

/// <summary>What other modules need to know about a plan. MonthlyPremium is the base rate before rating.</summary>
public record PlanInfo(
    int PolicyId,
    string PolicyName,
    PolicyType PolicyType,
    decimal CoverageAmount,
    decimal MonthlyPremium,
    int TermMonths,
    bool IsActive);
