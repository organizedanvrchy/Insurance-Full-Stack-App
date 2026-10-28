namespace Insurance.Identity.Validation;

public static class PhoneNumbers
{
    /// <summary>
    /// Accepts common US formats ("(203) 555-0141", "203-555-0141", "203.555.0141", "2035550141", "+1 203 555 0141")
    /// and returns them as "(203) 555-0141". Area code and exchange cannot start with 0 or 1 (North American numbering plan).
    /// </summary>
    public static bool TryNormalize(string? input, out string formatted)
    {
        formatted = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var text = input.Trim();
        if (!text.All(c => char.IsAsciiDigit(c) || " ()+-.".Contains(c))) return false;   // no letters or other symbols
        if (text.IndexOf('+') > 0) return false;                                           // "+" only as a prefix

        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1') digits = digits[1..];                 // drop the country code
        if (digits.Length != 10) return false;
        if (digits[0] is '0' or '1' || digits[3] is '0' or '1') return false;

        formatted = $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        return true;
    }
}