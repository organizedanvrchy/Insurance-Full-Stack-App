using Insurance.Enrollments.Models;

namespace Insurance.Enrollments.Repositories;

public interface IEnrollmentRepository
{
    Task<int> CreateAsync(int userId, int policyId, DateOnly startDate, int termMonths, decimal monthlyPremium);
    Task DeleteAsync(int enrollmentId);                     // compensation for a failed enrollment only
    Task<List<Enrollment>> GetByUserAsync(int userId);
    Task<Enrollment?> GetByIdAsync(int enrollmentId);
    Task<List<Enrollment>> GetManyAsync(IEnumerable<int> enrollmentIds);
    Task<Dictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds);
    Task<bool> CancelAsync(int enrollmentId, int userId);   // false => not found / not yours / not active
}
