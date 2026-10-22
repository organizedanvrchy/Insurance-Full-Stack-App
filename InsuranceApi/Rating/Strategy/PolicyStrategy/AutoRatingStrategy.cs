using InsuranceApi.Enums;

namespace InsuranceApi.Rating;

public class AutoRatingStrategy : RatingStrategy
{
    public override PolicyType PolicyType => PolicyType.Auto;

    public override IReadOnlyList<QuestionDefinition> Questions { get; } = new List<QuestionDefinition>
    {
        CommonQuestions.DateOfBirth(),
        new() { Key = "vehicleYear", Label = "Vehicle model year", Kind = QuestionType.Number, Min = 1990, Max = DateTime.UtcNow.Year + 1 },
        new() { Key = "atFaultAccidents", Label = "At-fault accidents in the last 5 years", Kind = QuestionType.Number, Min = 0, Max = 10 },
        new() { Key = "annualMileage", Label = "Miles driven per year", Kind = QuestionType.Choice,
                Options = new() { "Under 7,500", "7,500 to 15,000", "Over 15,000" } }
    };

    public override RatingResult Rate(decimal basePremium, AnswerSet a, DateOnly today)
    {
        var age = AgeOn(a.Date("dateOfBirth"), today);
        if (age < 18) throw Decline("drivers must be 18 or older.");

        var accidents = a.Int("atFaultAccidents");
        if (accidents >= 4) throw Decline("four or more at-fault accidents in the last 5 years.");

        var vehicleAge = Math.Max(0, today.Year - a.Int("vehicleYear"));
        var mileage = a.Choice("annualMileage");

        return Build(basePremium,
            new RatingFactor($"Driver age {age}", age switch { < 25 => 1.60m, < 65 => 1.00m, _ => 1.20m }),
            new RatingFactor($"Vehicle age {vehicleAge} yr", vehicleAge switch { <= 3 => 1.15m, <= 10 => 1.00m, _ => 0.90m }),
            new RatingFactor($"At-fault accidents: {accidents}", accidents switch { 0 => 1.00m, 1 => 1.30m, 2 => 1.70m, _ => 2.20m }),
            new RatingFactor($"Mileage: {mileage}", mileage switch { "Under 7,500" => 0.90m, "Over 15,000" => 1.20m, _ => 1.00m }));
    }
}