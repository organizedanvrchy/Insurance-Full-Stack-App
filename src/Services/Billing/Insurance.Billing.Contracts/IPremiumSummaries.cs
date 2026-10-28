using Insurance.BuildingBlocks.Http;

namespace Insurance.Billing.Contracts;

/// <summary>Payment progress per enrollment, for the Enrollments and Claims services (Billing's internal API).</summary>
public interface IPremiumSummaries
{
    /// <summary>One entry per requested enrollment, even if it has no installments.</summary>
    Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds);
}

/// <summary>Overdue uses the grace period. NextDue is the earliest unpaid installment, if any.</summary>
public record PremiumSummary(
    int PaymentsPaid,
    int PaymentsTotal,
    int OverdueCount,
    DateOnly? NextDueDate,
    decimal? NextDueAmount);

/// <summary>Billing created the premium schedule for an enrollment (a step of the enrollment saga).</summary>
public record PremiumScheduleCreated(int EnrollmentId);

/// <summary>Calls /internal/premiums on the Billing service.</summary>
public class PremiumSummariesClient : IPremiumSummaries
{
    private readonly HttpClient _http;
    public PremiumSummariesClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds)
    {
        var ids = enrollmentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, PremiumSummary>();

        var response = await _http.GetAsync($"internal/premiums/summaries?{ServiceClients.IdQuery("enrollmentIds", ids)}");
        return await response.ReadOrThrowAsync<Dictionary<int, PremiumSummary>>();
    }
}
