using Insurance.Quotes.Models;

namespace Insurance.Quotes.Repositories;

public interface IQuoteRepository
{
    Task<int> CreateAsync(int userId, int policyId, string answersJson, string breakdownJson,
        decimal basePremium, decimal monthlyPremium, DateTime expiresAtUtc);
    Task<Quote?> GetByIdAsync(int quoteId);
    Task<bool> MarkUsedAsync(int quoteId, int enrollmentId);   // false => already used
}