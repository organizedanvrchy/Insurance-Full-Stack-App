using System.Text.Json;
using InsuranceApi.DTOs;
using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Models;
using InsuranceApi.Rating;
using InsuranceApi.Repositories;

namespace InsuranceApi.Services;

public class QuoteService : IQuoteService
{
    private static readonly TimeSpan QuoteLifetime = TimeSpan.FromDays(30);

    private readonly IPolicyRepository _policies;
    private readonly IQuoteRepository _quotes;
    private readonly IReadOnlyDictionary<PolicyType, IRatingStrategy> _strategies;

    // DI hands us EVERY registered IRatingStrategy; we index them by the policy type they handle.
    public QuoteService(IPolicyRepository policies, IQuoteRepository quotes, IEnumerable<IRatingStrategy> strategies)
    {
        _policies = policies;
        _quotes = quotes;
        _strategies = strategies.ToDictionary(s => s.PolicyType);
    }

    public async Task<IReadOnlyList<QuestionDefinition>> GetQuestionsAsync(int policyId)
    {
        var policy = await GetActivePolicyAsync(policyId);
        return StrategyFor(policy).Questions;
    }

    public async Task<QuoteDto> CreateAsync(int userId, QuoteRequest request)
    {
        var policy = await GetActivePolicyAsync(request.PolicyId);
        var strategy = StrategyFor(policy);

        var answers = AnswerSet.From(strategy.Questions, request.Answers);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // The plan's MonthlyPremium is the BASE rate. A BusinessRuleException here means "declined".
        var result = strategy.Rate(policy.MonthlyPremium, answers, today);

        var id = await _quotes.CreateAsync(
            userId, policy.PolicyId,
            JsonSerializer.Serialize(answers.Values),
            JsonSerializer.Serialize(result.Factors),
            result.BasePremium, result.MonthlyPremium,
            DateTime.UtcNow.Add(QuoteLifetime));

        var saved = await _quotes.GetByIdAsync(id);
        return MapToDto(saved!);
    }

    public async Task<QuoteDto?> GetByIdAsync(int quoteId, int userId)
    {
        var quote = await _quotes.GetByIdAsync(quoteId);
        if (quote is null || quote.UserId != userId) return null;   // same null for "missing" and "not yours"
        return MapToDto(quote);
    }

    private async Task<Policy> GetActivePolicyAsync(int policyId)
    {
        var policy = await _policies.GetByIdAsync(policyId);
        if (policy is null || !policy.IsActive)
            throw new NotFoundException("That policy is not available.");
        return policy;
    }

    private IRatingStrategy StrategyFor(Policy policy) =>
        _strategies.TryGetValue(policy.PolicyType, out var strategy)
            ? strategy
            : throw new BusinessRuleException($"Online quotes aren't available for {policy.PolicyType} policies yet.");

    private static QuoteDto MapToDto(Quote q) => new()
    {
        QuoteId = q.QuoteId,
        PolicyId = q.PolicyId,
        PolicyName = q.PolicyName,
        PolicyType = q.PolicyType,
        CoverageAmount = q.CoverageAmount,
        TermMonths = q.TermMonths,
        BasePremium = q.BasePremium,
        MonthlyPremium = q.MonthlyPremium,
        TotalOverTerm = q.MonthlyPremium * q.TermMonths,
        Factors = JsonSerializer.Deserialize<List<RatingFactor>>(q.Breakdown) ?? new List<RatingFactor>(),
        ExpiresAt = q.ExpiresAt,
        IsExpired = q.ExpiresAt < DateTime.UtcNow,
        IsUsed = q.EnrollmentId is not null
    };
}