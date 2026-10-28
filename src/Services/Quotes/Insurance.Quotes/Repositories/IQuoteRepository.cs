using Insurance.Quotes.Contracts;
using Insurance.Quotes.Models;

namespace Insurance.Quotes.Repositories;

public interface IQuoteRepository
{
    Task<int> CreateAsync(int userId, int policyId, string answersJson, string breakdownJson,
        decimal basePremium, decimal monthlyPremium, DateTime expiresAtUtc);
    Task<Quote?> GetByIdAsync(int quoteId);

    Task<RedeemableQuote> ReserveAsync(int quoteId, int userId);   // throws NotFound / Conflict / BusinessRule
    Task ReleaseAsync(int quoteId);
    Task MarkUsedAsync(int quoteId, int enrollmentId);
}
