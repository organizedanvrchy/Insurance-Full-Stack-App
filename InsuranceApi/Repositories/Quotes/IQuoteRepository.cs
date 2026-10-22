using InsuranceApi.Models;

namespace InsuranceApi.Repositories;

public interface IQuoteRepository
{
    Task<int> CreateAsync(int userId, int policyId, string answersJson, string breakdownJson,
        decimal basePremium, decimal monthlyPremium, DateTime expiresAtUtc);
    Task<Quote?> GetByIdAsync(int quoteId);
}