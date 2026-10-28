using Insurance.Billing.Contracts;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Exceptions;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Repositories;
using Insurance.Quotes.Contracts;

namespace Insurance.Enrollments.Tests;

// Fakes are tiny in-memory stand-ins for the
// real repositories and for the other modules' contracts.
// The services only know the interfaces, so they
// can't tell the difference. That's dependency injection
// paying off: no database, no Azure, tests run in milliseconds.

public class FakeEnrollmentRepository : IEnrollmentRepository
{
    public List<Enrollment> Enrollments { get; } = new();
    public (int UserId, int PolicyId, DateOnly StartDate, int TermMonths, decimal MonthlyPremium)? LastCreate { get; private set; }
    public List<int> Deleted { get; } = new();
    private int _nextId = 100;

    public Task<int> CreateAsync(int userId, int policyId, DateOnly startDate, int termMonths, decimal monthlyPremium)
    {
        // Mimics the proc's "one active enrollment per plan" rule.
        if (Enrollments.Any(e => e.UserId == userId && e.PolicyId == policyId && e.Status == EnrollmentStatus.Active))
            throw new DuplicateEnrollmentException();

        LastCreate = (userId, policyId, startDate, termMonths, monthlyPremium);
        var enrollment = new Enrollment
        {
            EnrollmentId = _nextId++,
            UserId = userId,
            PolicyId = policyId,
            StartDate = startDate,
            EndDate = startDate.AddMonths(termMonths),
            MonthlyPremium = monthlyPremium,
            Status = EnrollmentStatus.Active
        };
        Enrollments.Add(enrollment);
        return Task.FromResult(enrollment.EnrollmentId);
    }

    public Task DeleteAsync(int enrollmentId)
    {
        Deleted.Add(enrollmentId);
        Enrollments.RemoveAll(e => e.EnrollmentId == enrollmentId);
        return Task.CompletedTask;
    }

    public Task<List<Enrollment>> GetByUserAsync(int userId) =>
        Task.FromResult(Enrollments.Where(e => e.UserId == userId).ToList());

    public Task<Enrollment?> GetByIdAsync(int enrollmentId) =>
        Task.FromResult(Enrollments.FirstOrDefault(e => e.EnrollmentId == enrollmentId));

    public Task<List<Enrollment>> GetManyAsync(IEnumerable<int> enrollmentIds) =>
        Task.FromResult(Enrollments.Where(e => enrollmentIds.Contains(e.EnrollmentId)).ToList());

    public Task<Dictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds) =>
        Task.FromResult(Enrollments
            .Where(e => userIds.Contains(e.UserId) && e.Status == EnrollmentStatus.Active)
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => g.Count()));

    public Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        var enrollment = Enrollments.FirstOrDefault(e =>
            e.EnrollmentId == enrollmentId && e.UserId == userId && e.Status == EnrollmentStatus.Active);

        if (enrollment is null) return Task.FromResult(false);

        enrollment.Status = EnrollmentStatus.Cancelled;
        return Task.FromResult(true);
    }
}

public class FakeQuoteRedemption : IQuoteRedemption
{
    // Every quote is valid for plan 1 at $100 unless a test says otherwise.
    public int PolicyId { get; set; } = 1;
    public Exception? RejectWith { get; set; }
    public bool FailMarkUsed { get; set; }

    public (int QuoteId, int UserId)? LastLookup { get; private set; }
    public (int QuoteId, int EnrollmentId)? MarkedUsed { get; private set; }

    public Task<RedeemableQuote> GetForEnrollmentAsync(int quoteId, int userId)
    {
        LastLookup = (quoteId, userId);
        if (RejectWith is not null) throw RejectWith;
        return Task.FromResult(new RedeemableQuote(quoteId, PolicyId, 100m));
    }

    public Task MarkUsedAsync(int quoteId, int enrollmentId)
    {
        if (FailMarkUsed) throw new ConflictException("This quote has already been used to enroll.");
        MarkedUsed = (quoteId, enrollmentId);
        return Task.CompletedTask;
    }
}

public class FakePlanCatalog : IPlanCatalog
{
    public Dictionary<int, PlanInfo> Plans { get; } = new()
    {
        [1] = new PlanInfo(1, "Test Plan", PolicyType.Auto, 50_000m, 80m, 12, IsActive: true)
    };

    public Task<PlanInfo?> GetAsync(int policyId) =>
        Task.FromResult(Plans.GetValueOrDefault(policyId));

    public Task<IReadOnlyDictionary<int, PlanInfo>> GetManyAsync(IEnumerable<int> policyIds) =>
        Task.FromResult<IReadOnlyDictionary<int, PlanInfo>>(
            Plans.Where(p => policyIds.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));
}

public class FakePremiumSchedule : IPremiumSchedule
{
    // Enrollments with no entry here have no installments at all.
    public Dictionary<int, PremiumSummary> Summaries { get; } = new();
    public List<int> Created { get; } = new();
    public List<int> RemovedUnpaid { get; } = new();
    public bool FailCreate { get; set; }

    public Task CreateAsync(int enrollmentId, DateOnly startDate, int termMonths, decimal monthlyPremium)
    {
        if (FailCreate) throw new InvalidOperationException("Billing is down.");
        Created.Add(enrollmentId);
        Summaries[enrollmentId] = new PremiumSummary(0, termMonths, 0, startDate, monthlyPremium);
        return Task.CompletedTask;
    }

    public Task RemoveUnpaidAsync(int enrollmentId)
    {
        RemovedUnpaid.Add(enrollmentId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds) =>
        Task.FromResult<IReadOnlyDictionary<int, PremiumSummary>>(enrollmentIds.Distinct().ToDictionary(
            id => id, id => Summaries.GetValueOrDefault(id) ?? new PremiumSummary(0, 0, 0, null, null)));
}
