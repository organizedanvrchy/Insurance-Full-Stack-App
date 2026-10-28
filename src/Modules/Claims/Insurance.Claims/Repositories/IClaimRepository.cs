using Insurance.Claims.Enums;
using Insurance.Claims.Models;

namespace Insurance.Claims.Repositories;

public interface IClaimRepository
{
    Task<int> SubmitAsync(int userId, int enrollmentId, DateOnly incidentDate, string description, decimal amount);
    Task<List<InsuranceClaim>> GetByUserAsync(int userId);
    Task<List<InsuranceClaim>> GetAllAsync(ClaimStatus? status);
    Task<InsuranceClaim?> GetByIdAsync(int claimId);
    Task<InsuranceClaim> ReviewAsync(int claimId, ClaimStatus from, ClaimStatus to, string? adminNotes, int reviewerId);
}