using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Models;
using Insurance.Quotes.Repositories;
using Insurance.Quotes.Services;
using Xunit;

namespace Insurance.Quotes.Tests;

// The quote rules the Enrollments module relies on when a customer enrolls.

public class QuoteRedemptionTests
{
    private readonly FakeQuoteRepository _repo = new();
    private readonly QuoteRedemption _sut;

    public QuoteRedemptionTests()
    {
        _sut = new QuoteRedemption(_repo);
        _repo.Quotes.Add(new Quote { QuoteId = 1, UserId = 1, PolicyId = 3, MonthlyPremium = 120m, ExpiresAt = DateTime.UtcNow.AddDays(10) });
    }

    [Fact]
    public async Task A_valid_quote_gives_its_plan_and_price()
    {
        var quote = await _sut.GetForEnrollmentAsync(1, userId: 1);

        Assert.Equal(3, quote.PolicyId);
        Assert.Equal(120m, quote.MonthlyPremium);
    }

    [Theory]
    [InlineData(99, 1)]   // no such quote
    [InlineData(1, 2)]    // someone else's
    public async Task Missing_and_foreign_quotes_look_the_same(int quoteId, int userId)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetForEnrollmentAsync(quoteId, userId));
    }

    [Fact]
    public async Task A_used_quote_is_a_conflict()
    {
        _repo.Quotes[0].EnrollmentId = 50;

        await Assert.ThrowsAsync<ConflictException>(() => _sut.GetForEnrollmentAsync(1, userId: 1));
    }

    [Fact]
    public async Task An_expired_quote_is_rejected()
    {
        _repo.Quotes[0].ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetForEnrollmentAsync(1, userId: 1));
    }

    [Fact]
    public async Task A_quote_can_only_be_marked_used_once()
    {
        await _sut.MarkUsedAsync(1, enrollmentId: 50);

        Assert.Equal(50, _repo.Quotes[0].EnrollmentId);
        await Assert.ThrowsAsync<ConflictException>(() => _sut.MarkUsedAsync(1, enrollmentId: 51));
    }
}

public class FakeQuoteRepository : IQuoteRepository
{
    public List<Quote> Quotes { get; } = new();

    public Task<int> CreateAsync(int userId, int policyId, string answersJson, string breakdownJson,
        decimal basePremium, decimal monthlyPremium, DateTime expiresAtUtc) => throw new NotSupportedException();

    public Task<Quote?> GetByIdAsync(int quoteId) =>
        Task.FromResult(Quotes.FirstOrDefault(q => q.QuoteId == quoteId));

    // Mimics the proc: only an unused quote can be marked.
    public Task<bool> MarkUsedAsync(int quoteId, int enrollmentId)
    {
        var quote = Quotes.FirstOrDefault(q => q.QuoteId == quoteId && q.EnrollmentId is null);
        if (quote is null) return Task.FromResult(false);
        quote.EnrollmentId = enrollmentId;
        return Task.FromResult(true);
    }
}
