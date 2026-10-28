using Insurance.Quotes.Contracts;
using Insurance.Quotes.Repositories;

namespace Insurance.Quotes.Services;

/// <summary>
/// The quote's part in the enrollment saga: hold it while the enrollment is set up (HTTP, from the Enrollments
/// service), then mark it used or release it when the saga's outcome arrives on the event bus.
/// Ownership, expiry and one-time use are checked inside usp_Quote_Reserve, with the row locked.
/// </summary>
public class QuoteReservations : IQuoteReservations
{
    private readonly IQuoteRepository _quotes;
    public QuoteReservations(IQuoteRepository quotes) => _quotes = quotes;

    public Task<RedeemableQuote> ReserveAsync(int quoteId, int userId) => _quotes.ReserveAsync(quoteId, userId);

    public Task ReleaseAsync(int quoteId) => _quotes.ReleaseAsync(quoteId);

    public Task MarkUsedAsync(int quoteId, int enrollmentId) => _quotes.MarkUsedAsync(quoteId, enrollmentId);
}
