using Insurance.Enrollments.Models;

namespace Insurance.Enrollments.Repositories;

public interface IEnrollmentRepository
{
    // Saves a Pending enrollment and its EnrollmentRequested event (outbox) in one transaction.
    Task<int> CreatePendingAsync(int userId, int policyId, int quoteId, DateOnly startDate, int termMonths, decimal monthlyPremium);
    Task<Enrollment?> CompleteAsync(int enrollmentId, bool succeeded);   // Pending -> Active / Failed

    Task<List<Enrollment>> GetByUserAsync(int userId);                   // never Pending or Failed
    Task<Enrollment?> GetByIdAsync(int enrollmentId);                    // never Failed
    Task<List<Enrollment>> GetManyAsync(IEnumerable<int> enrollmentIds); // never Failed
    Task<Dictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds);

    // Cancels and saves the EnrollmentCancelled event in one transaction. false => not found / not yours / not active.
    Task<bool> CancelAsync(int enrollmentId, int userId);
}
