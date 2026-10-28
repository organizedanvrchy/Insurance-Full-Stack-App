using Insurance.Catalog.Contracts;

namespace Insurance.Quotes.Rating;

public interface IRatingStrategy
{
    PolicyType PolicyType { get; }
    IReadOnlyList<QuestionDefinition> Questions { get; }

    /// <summary>
    /// Prices the policy, or throws BusinessRuleException 
    /// to decline the applicant.
    /// </summary>
    RatingResult Rate(decimal basePremium, AnswerSet answers, DateOnly today);
}