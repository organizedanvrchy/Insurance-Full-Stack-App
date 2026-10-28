using Insurance.Catalog.Contracts;
using Insurance.Quotes.Enums;

namespace Insurance.Quotes.Rating;

public class HomeRatingStrategy : RatingStrategy
{
    public override PolicyType PolicyType => PolicyType.Home;

    public override IReadOnlyList<QuestionDefinition> Questions { get; } = new List<QuestionDefinition>
    {
        new() { Key = "yearBuilt", Label = "Year the home was built", Kind = QuestionType.Number, Min = 1800, Max = DateTime.UtcNow.Year },
        new() { Key = "securitySystem", Label = "Does the home have a monitored security system?", Kind = QuestionType.Boolean },
        new() { Key = "priorClaims", Label = "Home insurance claims in the last 5 years", Kind = QuestionType.Number, Min = 0, Max = 10 }
    };

    public override RatingResult Rate(decimal basePremium, AnswerSet a, DateOnly today)
    {
        var claims = a.Int("priorClaims");
        if (claims >= 3) throw Decline("three or more claims in the last 5 years.");

        var homeAge = Math.Max(0, today.Year - a.Int("yearBuilt"));
        var secured = a.Bool("securitySystem");

        return Build(basePremium,
            new RatingFactor($"Home age {homeAge} yr", homeAge switch { <= 10 => 0.95m, <= 30 => 1.00m, <= 60 => 1.15m, _ => 1.35m }),
            new RatingFactor(secured ? "Monitored security system" : "No security system", secured ? 0.90m : 1.00m),
            new RatingFactor($"Claims in last 5 years: {claims}", claims switch { 0 => 1.00m, 1 => 1.20m, _ => 1.50m }));
    }
}