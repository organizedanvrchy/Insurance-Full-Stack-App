using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Contracts;
using Insurance.Quotes.Repositories;

namespace Insurance.Quotes.Services;

/// <summary>The quote rules enrolling depends on: ownership, expiry and one-time use.</summary>
public class QuoteRedemption : IQuoteRedemption
{
    private const string AlreadyUsed = "This quote has already been used to enroll.";

    private readonly IQuoteRepository _quotes;
    public QuoteRedemption(IQuoteRepository quotes) => _quotes = quotes;

    public async Task<RedeemableQuote> GetForEnrollmentAsync(int quoteId, int userId)
    {
        var quote = await _quotes.GetByIdAsync(quoteId);

        // Same answer for "missing" and "not yours", so ids can't be probed.
        if (quote is null || quote.UserId != userId)
            throw new NotFoundException("Quote not found.");

        if (quote.EnrollmentId is not null)
            throw new ConflictException(AlreadyUsed);

        if (quote.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("This quote has expired. Please request a new quote.");

        return new RedeemableQuote(quote.QuoteId, quote.PolicyId, quote.MonthlyPremium);
    }

    public async Task MarkUsedAsync(int quoteId, int enrollmentId)
    {
        if (!await _quotes.MarkUsedAsync(quoteId, enrollmentId))
            throw new ConflictException(AlreadyUsed);
    }
}
