namespace Insurance.Identity.Services;

/// <summary>
/// Builds the stored full name and address 
/// from the separate sign-up fields.
/// </summary>
public static class ContactFormatter
{
    public static string FullName(string firstName, string lastName) => $"{Squash(firstName)} {Squash(lastName)}";

    /// <summary>
    /// "48 Maple Avenue, Apt 3B, Stamford, CT 06902" 
    /// (the unit is skipped when empty).
    /// </summary>
    public static string Address(string street, string? unit, string city, string state, string postalCode)
    {
        var line1 = string.IsNullOrWhiteSpace(unit) ? Squash(street) : $"{Squash(street)}, {Squash(unit)}";
        return $"{line1}, {Squash(city)}, {state.Trim().ToUpperInvariant()} {postalCode.Trim()}";
    }

    // Trim and collapse repeated spaces: 
    // "  48   Maple  Ave " -> "48 Maple Ave"
    private static string Squash(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}