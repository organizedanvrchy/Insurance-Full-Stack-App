namespace Insurance.Enrollments.Contracts;

/// <summary>Read access to enrollments for other modules.</summary>
public interface IEnrollmentLookup
{
    Task<EnrollmentInfo?> GetAsync(int enrollmentId);   // null => no such enrollment
    Task<IReadOnlyDictionary<int, EnrollmentInfo>> GetManyAsync(IEnumerable<int> enrollmentIds);

    // Customers with no active policy are missing from the result.
    Task<IReadOnlyDictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds);
}

/// <summary>Status is the derived one: an Active enrollment past its end date reads as Expired.</summary>
public record EnrollmentInfo(
    int EnrollmentId,
    int UserId,
    int PolicyId,
    DateOnly StartDate,
    DateOnly EndDate,
    EnrollmentStatus Status);
