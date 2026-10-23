using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Validation;

/// <summary>
/// A two-letter US state code (50 states and DC), 
/// in any letter case.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class UsStateAttribute : ValidationAttribute
{
    private static readonly HashSet<string> States = new(StringComparer.OrdinalIgnoreCase)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY",
        "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH",
        "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
    };

    public UsStateAttribute() : base("Choose a valid US state.") { }

    public override bool IsValid(object? value) => value is string s && States.Contains(s.Trim());
}