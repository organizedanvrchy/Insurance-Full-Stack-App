using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Validation;

/// <summary>
/// An optional US phone number: empty is fine, 
/// anything entered must be valid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class UsPhoneAttribute : ValidationAttribute
{
    public UsPhoneAttribute() : base("Enter a valid US phone number, for example (203) 555-0141.") { }

    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || PhoneNumbers.TryNormalize(s, out _);
}