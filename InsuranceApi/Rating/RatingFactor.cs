namespace InsuranceApi.Rating;

/// <summary>
/// One line of the price breakdown, 
/// e.g. ("Driver age 22", 1.60).
/// </summary>
public record RatingFactor(string Label, decimal Multiplier);