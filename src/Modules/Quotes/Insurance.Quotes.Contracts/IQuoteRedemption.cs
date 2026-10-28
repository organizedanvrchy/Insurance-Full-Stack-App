namespace Insurance.Quotes.Contracts;

/// <summary>Turning a quote into a policy. Used by the Enrollments module.</summary>
public interface IQuoteRedemption
{
    /// <summary>
    /// The quote, if this customer can still enroll with it. Throws NotFoundException (missing or not theirs),
    /// ConflictException (already used) or BusinessRuleException (expired).
    /// </summary>
    Task<RedeemableQuote> GetForEnrollmentAsync(int quoteId, int userId);

    /// <summary>Marks the quote used by this enrollment. Throws ConflictException if another enrollment got there first.</summary>
    Task MarkUsedAsync(int quoteId, int enrollmentId);
}

public record RedeemableQuote(int QuoteId, int PolicyId, decimal MonthlyPremium);
