namespace InsuranceApi.Rating;

public record RatingResult(decimal BasePremium, decimal MonthlyPremium, IReadOnlyList<RatingFactor> Factors);