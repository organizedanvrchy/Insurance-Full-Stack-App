using Insurance.Billing.Contracts;
using Insurance.Billing.Repositories;

namespace Insurance.Billing.Services;

/// <summary>Implements the module's public contract on top of its own repository.</summary>
public class PremiumSchedule : IPremiumSchedule
{
    private readonly IPaymentRepository _payments;
    public PremiumSchedule(IPaymentRepository payments) => _payments = payments;

    public Task CreateAsync(int enrollmentId, DateOnly startDate, int termMonths, decimal monthlyPremium) =>
        _payments.CreateScheduleAsync(enrollmentId, startDate, termMonths, monthlyPremium);

    public Task RemoveUnpaidAsync(int enrollmentId) =>
        _payments.RemoveUnpaidAsync(enrollmentId);

    public async Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds) =>
        await _payments.GetSummariesAsync(enrollmentIds);
}
