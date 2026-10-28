using Insurance.BuildingBlocks.Http;

namespace Insurance.Enrollments.Contracts;

/// <summary>Read access to enrollments for other services (Enrollments' internal API).</summary>
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

/// <summary>Calls /internal/enrollments on the Enrollments service.</summary>
public class EnrollmentLookupClient : IEnrollmentLookup
{
    private readonly HttpClient _http;
    public EnrollmentLookupClient(HttpClient http) => _http = http;

    public async Task<EnrollmentInfo?> GetAsync(int enrollmentId) =>
        (await GetManyAsync(new[] { enrollmentId })).GetValueOrDefault(enrollmentId);

    public async Task<IReadOnlyDictionary<int, EnrollmentInfo>> GetManyAsync(IEnumerable<int> enrollmentIds)
    {
        var ids = enrollmentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, EnrollmentInfo>();

        var response = await _http.GetAsync($"internal/enrollments?{ServiceClients.IdQuery("ids", ids)}");
        return (await response.ReadOrThrowAsync<List<EnrollmentInfo>>()).ToDictionary(e => e.EnrollmentId);
    }

    public async Task<IReadOnlyDictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        var response = await _http.GetAsync($"internal/enrollments/active-counts?{ServiceClients.IdQuery("userIds", ids)}");
        return await response.ReadOrThrowAsync<Dictionary<int, int>>();
    }
}
