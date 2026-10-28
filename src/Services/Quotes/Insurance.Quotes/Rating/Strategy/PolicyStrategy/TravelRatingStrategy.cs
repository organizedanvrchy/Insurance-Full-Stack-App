using Insurance.Catalog.Contracts;
using Insurance.Quotes.Enums;

namespace Insurance.Quotes.Rating;

public class TravelRatingStrategy : RatingStrategy
{
    public override PolicyType PolicyType => PolicyType.Travel;

    public override IReadOnlyList<QuestionDefinition> Questions { get; } = new List<QuestionDefinition>
    {
        CommonQuestions.DateOfBirth(),
        new() { Key = "tripLengthDays", Label = "Trip length (days)", Kind = QuestionType.Number, Min = 1, Max = 365 },
        new() { Key = "destination", Label = "Destination", Kind = QuestionType.Choice,
                Options = new() { "Domestic", "International" } }
    };

    public override RatingResult Rate(decimal basePremium, AnswerSet a, DateOnly today)
    {
        var age = AgeOn(a.Date("dateOfBirth"), today);
        if (age < 18) throw Decline("travelers must be 18 or older.");

        var days = a.Int("tripLengthDays");
        var destination = a.Choice("destination");

        return Build(basePremium,
            new RatingFactor($"Traveler age {age}", age switch { < 30 => 0.90m, < 60 => 1.00m, _ => 1.40m }),
            new RatingFactor($"Trip length {days} days", days switch { <= 14 => 1.00m, <= 30 => 1.25m, _ => 1.60m }),
            new RatingFactor($"Destination: {destination}", destination == "International" ? 1.45m : 1.00m));
    }
}