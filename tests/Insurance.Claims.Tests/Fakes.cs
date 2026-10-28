using Insurance.Claims.Enums;
using Insurance.Claims.Models;
using Insurance.Claims.Repositories;

namespace Insurance.Claims.Tests;

// Fakes are tiny in-memory stand-ins for the 
// real repositories. The services only know the
// interfaces (IUserRepository and so on), so they 
// can't tell the difference. That's dependency injection 
// paying off: no database, no Azure, tests run in milliseconds.

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
