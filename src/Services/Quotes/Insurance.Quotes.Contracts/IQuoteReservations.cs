using System.Net.Http.Json;
using Insurance.BuildingBlocks.Http;

namespace Insurance.Quotes.Contracts;

/// <summary>
/// The enrollment saga's first step: hold a quote while the enrollment is being set up.
/// The Quotes service turns the hold into a permanent "used" when it receives EnrollmentActivated,
/// and drops it when it receives EnrollmentFailed (or when the hold times out).
/// </summary>
public interface IQuoteReservations
{
    /// <summary>
    /// Holds the quote for this customer. Throws NotFoundException (missing or not theirs),
    /// ConflictException (already used or held) or BusinessRuleException (expired).
    /// </summary>
    Task<RedeemableQuote> ReserveAsync(int quoteId, int userId);

    /// <summary>The enrollment won't go ahead (for example the plan was retired): the quote can be used again.</summary>
    Task ReleaseAsync(int quoteId);
}

public record RedeemableQuote(int QuoteId, int PolicyId, decimal MonthlyPremium);

/// <summary>Calls /internal/quotes on the Quotes service.</summary>
public class QuoteReservationsClient : IQuoteReservations
{
    private readonly HttpClient _http;
    public QuoteReservationsClient(HttpClient http) => _http = http;

    public async Task<RedeemableQuote> ReserveAsync(int quoteId, int userId)
    {
        var response = await _http.PostAsJsonAsync($"internal/quotes/{quoteId}/reserve", new { userId }, ServiceClients.Json);
        return await response.ReadOrThrowAsync<RedeemableQuote>();
    }

    public async Task ReleaseAsync(int quoteId)
    {
        var response = await _http.PostAsync($"internal/quotes/{quoteId}/release", null);
        await response.ThrowIfFailedAsync();
    }
}
