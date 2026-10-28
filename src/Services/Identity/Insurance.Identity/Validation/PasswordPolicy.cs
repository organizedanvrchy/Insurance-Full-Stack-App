namespace Insurance.Identity.Validation;

/// <summary>
/// The password rules, in one place. 
/// Change MinLength here to change the minimum.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 72;   // BCrypt only uses the first 72 bytes

    /// <summary>
    /// The rules this password does not meet yet, as short phrases. 
    /// Empty means it is acceptable.
    /// </summary>
    public static IReadOnlyList<string> UnmetRules(string? password)
    {
        var p = password ?? string.Empty;
        var unmet = new List<string>();

        if (p.Length < MinLength) unmet.Add($"at least {MinLength} characters");
        if (!p.Any(char.IsUpper)) unmet.Add("an uppercase letter");
        // Special = anything that is not a letter, digit or whitespace
        if (!p.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) unmet.Add("a special character");

        return unmet;
    }
}