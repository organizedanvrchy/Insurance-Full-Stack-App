using System.ComponentModel.DataAnnotations;

namespace Insurance.Identity.Validation;

/// <summary>
/// Validates a password against <see cref="PasswordPolicy"/>. 
/// Used on RegisterRequest (never on login).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "") return ValidationResult.Success;   // [Required] reports missing values

        var unmet = PasswordPolicy.UnmetRules(value as string);
        if (unmet.Count == 0) return ValidationResult.Success;

        var members = validationContext.MemberName is { } name ? new[] { name } : null;
        return new ValidationResult($"Password needs {string.Join(", ", unmet)}.", members);
    }
}