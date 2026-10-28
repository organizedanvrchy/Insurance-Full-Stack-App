using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Repositories;

namespace Insurance.Enrollments.Tests;

// Fakes are tiny in-memory stand-ins for the 
// real repositories. The services only know the
// interfaces (IUserRepository and so on), so they 
// can't tell the difference. That's dependency injection 
// paying off: no database, no Azure, tests run in milliseconds.

public class FakeEnrollmentRepository : IEnrollmentRepository
{
    public List<Enrollment> Enrollments { get; } = new();
    public (int UserId, int QuoteId, DateOnly StartDate)? LastCreate { get; private set; }
    private int _nextId = 100;

    public Task<int> CreateAsync(int userId, int quoteId, DateOnly startDate)
    {
        LastCreate = (userId, quoteId, startDate);
        var enrollment = new Enrollment
        {
            EnrollmentId = _nextId++,
            UserId = userId,
            PolicyId = 1,
            PolicyName = "Test Plan",
            StartDate = startDate,
            EndDate = startDate.AddMonths(12),
            MonthlyPremium = 100m,
            Status = EnrollmentStatus.Active,
            PaymentsTotal = 12
        };
        Enrollments.Add(enrollment);
        return Task.FromResult(enrollment.EnrollmentId);
    }

    public Task<List<Enrollment>> GetByUserAsync(int userId) =>
        Task.FromResult(Enrollments.Where(e => e.UserId == userId).ToList());

    public Task<Enrollment?> GetByIdAsync(int enrollmentId) =>
        Task.FromResult(Enrollments.FirstOrDefault(e => e.EnrollmentId == enrollmentId));

    public Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        var enrollment = Enrollments.FirstOrDefault(e =>
            e.EnrollmentId == enrollmentId && e.UserId == userId && e.Status == EnrollmentStatus.Active);

        if (enrollment is null) return Task.FromResult(false);

        enrollment.Status = EnrollmentStatus.Cancelled;
        return Task.FromResult(true);
    }
}
