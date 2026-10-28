using Insurance.Catalog.Contracts;

namespace Insurance.Quotes.Rating;

public class HealthRatingStrategy : RatingStrategy
{
    public override PolicyType PolicyType => PolicyType.Health;

    public override IReadOnlyList<QuestionDefinition> Questions { get; } = new List<QuestionDefinition>
    {
        CommonQuestions.DateOfBirth(),
        CommonQuestions.Tobacco(),
        CommonQuestions.Conditions()
    };

    public override RatingResult Rate(decimal basePremium, AnswerSet a, DateOnly today)
    {
        var age = AgeOn(a.Date("dateOfBirth"), today);
        if (age < 18) throw Decline("applicants must be 18 or older.");

        var smoker = a.Bool("tobaccoUse");
        var conditions = a.Choice("conditions");

        return Build(basePremium,
            new RatingFactor($"Age {age}", age switch { < 30 => 0.85m, < 45 => 1.00m, < 60 => 1.35m, _ => 1.80m }),
            new RatingFactor(smoker ? "Tobacco use" : "Non-tobacco user", smoker ? 1.40m : 1.00m),
            new RatingFactor($"Medical conditions: {conditions}", conditions switch { "Minor" => 1.15m, "Major" => 1.45m, _ => 1.00m }));
    }
}