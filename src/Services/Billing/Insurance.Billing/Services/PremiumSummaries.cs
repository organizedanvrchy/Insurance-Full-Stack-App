using Insurance.Billing.Contracts;
using Insurance.Billing.Repositories;

namespace Insurance.Billing.Services;

/// <summary>Implements the service's internal API on top of its own repository.</summary>
public class PremiumSummaries : IPremiumSummaries
{
    private readonly IPaymentRepository _payments;
    public PremiumSummaries(IPaymentRepository payments) => _payments = payments;

    public async Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds) =>
        await _payments.GetSummariesAsync(enrollmentIds);
}
