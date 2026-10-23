using System.ComponentModel.DataAnnotations;
using InsuranceApi.DTOs;
using InsuranceApi.Services;
using InsuranceApi.Validation;
using Xunit;

namespace InsuranceApi.Tests;

public class RegistrationRulesTests
{
    private static RegisterRequest Valid() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@example.com",
        Password = "Str0ng!Pass",
        Phone = "(203) 555-0141",
        Street = "48 Maple Avenue",
        City = "Stamford",
        State = "CT",
        PostalCode = "06902"
    };

    // Runs the same attribute validation ASP.NET Core runs before the controller is called
    private static List<ValidationResult> Validate(RegisterRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static bool Flags(List<ValidationResult> results, string member) =>
        results.Any(r => r.MemberNames.Contains(member));

    [Fact]
    public void A_complete_request_is_valid()
    {
        Assert.Empty(Validate(Valid()));
    }

    // ---------------- Password ----------------

    [Theory]
    [InlineData("Str0ng!Pass", true)]
    [InlineData("Abcdefg!", true)]       // exactly 8 characters is allowed
    [InlineData("ALLCAPS!!", true)]      // no lowercase rule
    [InlineData("Abcdef!", false)]       // 7 characters
    [InlineData("weakpass1!", false)]    // no uppercase
    [InlineData("NoSpecial1", false)]    // no special character
    [InlineData("password", false)]      // fails all three
    public void Password_rules(string password, bool expectedValid)
    {
        var request = Valid();
        request.Password = password;

        Assert.Equal(expectedValid, !Flags(Validate(request), nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void The_password_message_names_what_is_missing()
    {
        var unmet = PasswordPolicy.UnmetRules("weakpass");

        Assert.Equal(new[] { "an uppercase letter", "a special character" }, unmet);
    }

    // ---------------- Phone ----------------

    [Theory]
    [InlineData("(203) 555-0141")]
    [InlineData("203-555-0141")]
    [InlineData("203.555.0141")]
    [InlineData("2035550141")]
    [InlineData("+1 203 555 0141")]
    [InlineData("1-203-555-0141")]
    public void Common_us_formats_normalize_to_one_format(string input)
    {
        Assert.True(PhoneNumbers.TryNormalize(input, out var formatted));
        Assert.Equal("(203) 555-0141", formatted);
    }

    [Theory]
    [InlineData("555-0141")]              // no area code
    [InlineData("123-456-7890")]          // area code can't start with 1
    [InlineData("(203) 155-0141")]        // exchange can't start with 1
    [InlineData("203-555-01411")]         // too long
    [InlineData("call me")]
    [InlineData("203-555-0141 ext 5")]    // letters
    [InlineData("+44 20 7946 0958")]      // not a US number
    public void Invalid_phone_numbers_are_rejected(string input)
    {
        Assert.False(PhoneNumbers.TryNormalize(input, out _));

        var request = Valid();
        request.Phone = input;
        Assert.True(Flags(Validate(request), nameof(RegisterRequest.Phone)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void The_phone_is_optional(string? input)
    {
        var request = Valid();
        request.Phone = input;

        Assert.False(Flags(Validate(request), nameof(RegisterRequest.Phone)));
    }

    // ---------------- Names and address ----------------

    [Theory]
    [InlineData("O'Brien", true)]
    [InlineData("Anne-Marie", true)]
    [InlineData("José", true)]
    [InlineData("J4ne", false)]
    [InlineData("Jane<script>", false)]
    public void Names_allow_letters_hyphens_and_apostrophes_only(string name, bool expectedValid)
    {
        var request = Valid();
        request.FirstName = name;

        Assert.Equal(expectedValid, !Flags(Validate(request), nameof(RegisterRequest.FirstName)));
    }

    [Theory]
    [InlineData("CT", true)]
    [InlineData("ct", true)]
    [InlineData("DC", true)]
    [InlineData("ZZ", false)]
    [InlineData("Connecticut", false)]
    public void State_must_be_a_us_state_code(string state, bool expectedValid)
    {
        var request = Valid();
        request.State = state;

        Assert.Equal(expectedValid, !Flags(Validate(request), nameof(RegisterRequest.State)));
    }

    [Theory]
    [InlineData("06902", true)]
    [InlineData("06902-1234", true)]
    [InlineData("6902", false)]
    [InlineData("ABCDE", false)]
    public void Zip_code_must_be_five_digits(string zip, bool expectedValid)
    {
        var request = Valid();
        request.PostalCode = zip;

        Assert.Equal(expectedValid, !Flags(Validate(request), nameof(RegisterRequest.PostalCode)));
    }

    [Fact]
    public void Missing_required_fields_are_flagged()
    {
        var results = Validate(new RegisterRequest());

        foreach (var member in new[] { "FirstName", "LastName", "Email", "Password", "Street", "City", "State", "PostalCode" })
            Assert.True(Flags(results, member), $"{member} should be required");
        Assert.False(Flags(results, "Unit"));    // optional
        Assert.False(Flags(results, "Phone"));   // optional
    }

    // ---------------- Joining ----------------

    [Fact]
    public void Full_name_trims_and_collapses_spaces()
    {
        Assert.Equal("Jane Doe", ContactFormatter.FullName("  Jane ", "  Doe "));
    }

    [Theory]
    [InlineData("Apt 3B", "48 Maple Avenue, Apt 3B, Stamford, CT 06902")]
    [InlineData(null, "48 Maple Avenue, Stamford, CT 06902")]
    [InlineData("   ", "48 Maple Avenue, Stamford, CT 06902")]
    public void Address_skips_an_empty_unit(string? unit, string expected)
    {
        Assert.Equal(expected, ContactFormatter.Address("48 Maple Avenue", unit, "Stamford", "ct", "06902"));
    }
}