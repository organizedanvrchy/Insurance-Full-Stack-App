using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Repositories;

namespace Insurance.Enrollments.Services;

/// <summary>Implements the service's internal API on top of its own repository.</summary>
public class EnrollmentLookup : IEnrollmentLookup
{
    private readonly IEnrollmentRepository _enrollments;
    public EnrollmentLookup(IEnrollmentRepository enrollments) => _enrollments = enrollments;

    public async Task<EnrollmentInfo?> GetAsync(int enrollmentId)
    {
        var e = await _enrollments.GetByIdAsync(enrollmentId);
        return e is null ? null : ToInfo(e);
    }

    public async Task<IReadOnlyDictionary<int, EnrollmentInfo>> GetManyAsync(IEnumerable<int> enrollmentIds) =>
        (await _enrollments.GetManyAsync(enrollmentIds)).ToDictionary(e => e.EnrollmentId, ToInfo);

    public async Task<IReadOnlyDictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds) =>
        await _enrollments.CountActiveByUserAsync(userIds);

    private static EnrollmentInfo ToInfo(Enrollment e) =>
        new(e.EnrollmentId, e.UserId, e.PolicyId, e.StartDate, e.EndDate, e.Status);
}
