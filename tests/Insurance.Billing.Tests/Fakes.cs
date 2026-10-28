using Insurance.Billing.Contracts;
using Insurance.Billing.Enums;
using Insurance.Billing.Models;
using Insurance.Billing.Repositories;
using Insurance.Enrollments.Contracts;

namespace Insurance.Billing.Tests;

// Fakes are tiny in-memory stand-ins for the 
// real repositories and for the other modules' contracts.
// The services only know the interfaces, so they 
// can't tell the difference. That's dependency injection 
// paying off: no database, no Azure, tests run in milliseconds.

public class FakePaymentRepository : IPaymentRepository
{
    public List<Payment> Payments { get; } = new();
    public int? LastPaid { get; private set; }

    public Task<List<Payment>> GetByEnrollmentAsync(int enrollmentId) =>
        Task.FromResult(Payments.Where(p => p.EnrollmentId == enrollmentId).OrderBy(p => p.DueDate).ToList());

    public Task<Payment?> GetByIdAsync(int paymentId) =>
        Task.FromResult(Payments.FirstOrDefault(p => p.PaymentId == paymentId));

    public Task<Payment> PayAsync(int paymentId)
    {
        LastPaid = paymentId;
        var payment = Payments.Single(p => p.PaymentId == paymentId);
        payment.PaidDate = DateOnly.FromDateTime(DateTime.UtcNow);
        payment.Status = PaymentStatus.Paid;
        return Task.FromResult(payment);
    }

    public Task CreateScheduleAsync(int enrollmentId, DateOnly startDate, int termMonths, decimal monthlyPremium) =>
        throw new NotSupportedException();

    public Task RemoveUnpaidAsync(int enrollmentId) => throw new NotSupportedException();

    public Task<Dictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds) =>
        throw new NotSupportedException();
}

public class FakeEnrollmentLookup : IEnrollmentLookup
{
    public Dictionary<int, EnrollmentInfo> Enrollments { get; } = new();

    public Task<EnrollmentInfo?> GetAsync(int enrollmentId) =>
        Task.FromResult(Enrollments.GetValueOrDefault(enrollmentId));

    public Task<IReadOnlyDictionary<int, EnrollmentInfo>> GetManyAsync(IEnumerable<int> enrollmentIds) =>
        throw new NotSupportedException();

    public Task<IReadOnlyDictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds) =>
        throw new NotSupportedException();
}
