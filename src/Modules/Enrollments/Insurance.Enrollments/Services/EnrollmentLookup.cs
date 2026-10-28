using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Repositories;

namespace Insurance.Enrollments.Services;

/// <summary>Implements the module's public contract on top of its own repository.</summary>
public class EnrollmentLookup : IEnrollmentLookup
{
    private readonly IEnrollmentRepository _enrollments;
    public EnrollmentLookup(IEnrollmentRepository enrollments) => _enrollments = enrollments;

    public async Task<EnrollmentInfo?> GetAsync(int enrollmentId)
    {
        var e = await _enrollments.GetByIdAsync(enrollmentId);
        return e is null
            ? null
            : new EnrollmentInfo(e.EnrollmentId, e.UserId, e.PolicyId, e.StartDate, e.EndDate, e.Status);
    }
}
