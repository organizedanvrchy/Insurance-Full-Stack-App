using InsuranceApi.Enums;

namespace InsuranceApi.Rating;

public class LifeRatingStrategy : RatingStrategy
{
    public override PolicyType PolicyType => PolicyType.Life;

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
        if (age >= 70) throw Decline("term life cover is not available online from age 70.");

        var smoker = a.Bool("tobaccoUse");
        var conditions = a.Choice("conditions");

        return Build(basePremium,
            new RatingFactor($"Age {age}", age switch { < 30 => 0.70m, < 45 => 1.00m, < 60 => 2.00m, _ => 3.50m }),
            new RatingFactor(smoker ? "Tobacco use" : "Non-tobacco user", smoker ? 1.90m : 1.00m),
            new RatingFactor($"Medical conditions: {conditions}", conditions switch { "Minor" => 1.25m, "Major" => 1.80m, _ => 1.00m }));
    }
}