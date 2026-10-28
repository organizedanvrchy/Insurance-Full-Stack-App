using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;

namespace Insurance.Quotes.Rating;

public abstract class RatingStrategy : IRatingStrategy
{
    public abstract PolicyType PolicyType { get; }
    public abstract IReadOnlyList<QuestionDefinition> Questions { get; }
    public abstract RatingResult Rate(decimal basePremium, AnswerSet answers, DateOnly today);

    /// <summary>
    /// premium = base x every factor, 
    /// rounded to cents, never below $1.00
    /// </summary>
    protected static RatingResult Build(decimal basePremium, params RatingFactor[] factors)
    {
        var premium = basePremium;
        foreach (var f in factors) premium *= f.Multiplier;
        premium = Math.Max(1.00m, Math.Round(premium, 2, MidpointRounding.AwayFromZero));
        return new RatingResult(basePremium, premium, factors);
    }

    protected static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;

        if (age < 0 || age > 110)
            throw new BusinessRuleException("Date of birth: please enter a valid date.");
        return age;
    }

    /// <summary>
    /// Underwriting decline. Usage: 
    /// throw Decline("reason");
    /// </summary>
    protected static BusinessRuleException Decline(string reason) =>
        new($"We're unable to offer this coverage online: {reason}");
}