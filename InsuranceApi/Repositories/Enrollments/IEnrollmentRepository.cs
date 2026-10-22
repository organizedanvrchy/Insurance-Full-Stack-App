using InsuranceApi.Models;

namespace InsuranceApi.Repositories;

public interface IEnrollmentRepository
{
    Task<int> CreateAsync(int userId, int quoteId, DateOnly startDate);
    Task<List<Enrollment>> GetByUserAsync(int userId);
    Task<Enrollment?> GetByIdAsync(int enrollmentId);
    Task<bool> CancelAsync(int enrollmentId, int userId);   // false => not found / not yours / not active
}