using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Models;
using InsuranceApi.Repositories;
using InsuranceApi.Services;

namespace InsuranceApi.Tests;

// Fakes are tiny in-memory stand-ins for the 
// real repositories. The services only know the
// interfaces (IUserRepository and so on), so they 
// can't tell the difference. That's dependency injection 
// paying off: no database, no Azure, tests run in milliseconds.

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = new();
    private int _nextId = 1;

    public Task<int> CreateAsync(User user)
    {
        // Mimics what the real repository does when SQL reports a duplicate email.
        if (Users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateEmailException(user.Email);

        user.UserId = _nextId++;
        Users.Add(user);
        return Task.FromResult(user.UserId);
    }

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(int userId) =>
        Task.FromResult(Users.FirstOrDefault(u => u.UserId == userId));
}

public class FakeTokenService : ITokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user) =>
        ($"token-for-{user.UserId}", DateTime.UtcNow.AddHours(1));
}

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

public class FakeClaimRepository : IClaimRepository
{
    public List<InsuranceClaim> Claims { get; } = new();

    // Remembers how the service called ReviewAsync, so tests can check the arguments.
    public (int ClaimId, ClaimStatus From, ClaimStatus To, string? Notes, int ReviewerId)? LastReview { get; private set; }

    public Task<int> SubmitAsync(int userId, int enrollmentId, DateOnly incidentDate, string description, decimal amount)
    {
        var claim = new InsuranceClaim
        {
            ClaimId = Claims.Count + 1,
            ClaimNumber = $"CLM-TEST-{Claims.Count + 1:000000}",
            UserId = userId,
            EnrollmentId = enrollmentId,
            IncidentDate = incidentDate,
            Description = description,
            ClaimAmount = amount,
            Status = ClaimStatus.Submitted
        };
        Claims.Add(claim);
        return Task.FromResult(claim.ClaimId);
    }

    public Task<List<InsuranceClaim>> GetByUserAsync(int userId) =>
        Task.FromResult(Claims.Where(c => c.UserId == userId).ToList());

    public Task<List<InsuranceClaim>> GetAllAsync(ClaimStatus? status) =>
        Task.FromResult(Claims.Where(c => status == null || c.Status == status).ToList());

    public Task<InsuranceClaim?> GetByIdAsync(int claimId) =>
        Task.FromResult(Claims.FirstOrDefault(c => c.ClaimId == claimId));

    public Task<InsuranceClaim> ReviewAsync(int claimId, ClaimStatus from, ClaimStatus to, string? adminNotes, int reviewerId)
    {
        LastReview = (claimId, from, to, adminNotes, reviewerId);

        var claim = Claims.Single(c => c.ClaimId == claimId);
        claim.Status = to;
        claim.AdminNotes = adminNotes ?? claim.AdminNotes;
        return Task.FromResult(claim);
    }
}