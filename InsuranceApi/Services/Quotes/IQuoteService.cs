using InsuranceApi.DTOs;
using InsuranceApi.Rating;

namespace InsuranceApi.Services;

public interface IQuoteService
{
    Task<IReadOnlyList<QuestionDefinition>> GetQuestionsAsync(int policyId);
    Task<QuoteDto> CreateAsync(int userId, QuoteRequest request);
    Task<QuoteDto?> GetByIdAsync(int quoteId, int userId);   // null => 404
}