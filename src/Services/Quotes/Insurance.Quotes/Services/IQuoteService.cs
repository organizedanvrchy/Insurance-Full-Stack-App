using Insurance.Quotes.DTOs;
using Insurance.Quotes.Rating;

namespace Insurance.Quotes.Services;

public interface IQuoteService
{
    Task<IReadOnlyList<QuestionDefinition>> GetQuestionsAsync(int policyId);
    Task<QuoteDto> CreateAsync(int userId, QuoteRequest request);
    Task<QuoteDto?> GetByIdAsync(int quoteId, int userId);   // null => 404
}