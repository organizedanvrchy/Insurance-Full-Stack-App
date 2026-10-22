using InsuranceApi.DTOs;
using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Services;
using Xunit;

namespace InsuranceApi.Tests;

public class AuthServiceTests
{
    // xUnit creates a NEW instance of this class for 
    // every test, so each test starts with an empty fake.
    private readonly FakeUserRepository _users = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, new FakeTokenService());
    }

    private static RegisterRequest NewRegistration(string email = "jane.doe@example.com") => new()
    {
        FullName = "  Jane Doe  ",
        Email = email,
        Password = "Str0ngPass!"
    };

    // Register

    [Fact]
    public async Task Register_stores_a_hash_never_the_plain_password()
    {
        await _sut.RegisterAsync(NewRegistration());

        var saved = _users.Users.Single();
        Assert.NotEqual("Str0ngPass!", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Str0ngPass!", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_always_creates_a_customer()
    {
        await _sut.RegisterAsync(NewRegistration());

        Assert.Equal(UserRole.Customer, _users.Users.Single().Role);
    }

    [Fact]
    public async Task Register_trims_the_name_and_lowercases_the_email()
    {
        await _sut.RegisterAsync(NewRegistration("  Jane.Doe@Example.COM "));

        var saved = _users.Users.Single();
        Assert.Equal("Jane Doe", saved.FullName);
        Assert.Equal("jane.doe@example.com", saved.Email);
    }

    [Fact]
    public async Task Register_returns_a_token_and_the_new_user()
    {
        var result = await _sut.RegisterAsync(NewRegistration());

        Assert.Equal("token-for-1", result.Token);
        Assert.Equal("jane.doe@example.com", result.User.Email);
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email_even_in_a_different_case()
    {
        await _sut.RegisterAsync(NewRegistration("jane.doe@example.com"));

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _sut.RegisterAsync(NewRegistration("JANE.DOE@EXAMPLE.COM")));
    }

    // Login

    [Fact]
    public async Task Login_succeeds_with_the_right_password()
    {
        await _sut.RegisterAsync(NewRegistration());

        var result = await _sut.LoginAsync(new LoginRequest { Email = "jane.doe@example.com", Password = "Str0ngPass!" });

        Assert.NotNull(result);
        Assert.Equal("token-for-1", result!.Token);
    }

    [Fact]
    public async Task Login_ignores_email_capitalization()
    {
        await _sut.RegisterAsync(NewRegistration());

        var result = await _sut.LoginAsync(new LoginRequest { Email = "JANE.Doe@Example.com", Password = "Str0ngPass!" });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Login_fails_with_the_wrong_password()
    {
        await _sut.RegisterAsync(NewRegistration());

        var result = await _sut.LoginAsync(new LoginRequest { Email = "jane.doe@example.com", Password = "WrongPassword1" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_email()
    {
        var result = await _sut.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "Str0ngPass!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_fails_for_a_deactivated_account_even_with_the_right_password()
    {
        await _sut.RegisterAsync(NewRegistration());
        _users.Users.Single().IsActive = false;

        var result = await _sut.LoginAsync(new LoginRequest { Email = "jane.doe@example.com", Password = "Str0ngPass!" });

        Assert.Null(result);
    }

    // Profile

    [Fact]
    public async Task Profile_is_returned_for_an_active_user()
    {
        await _sut.RegisterAsync(NewRegistration());

        var profile = await _sut.GetProfileAsync(1);

        Assert.NotNull(profile);
        Assert.Equal("Jane Doe", profile!.FullName);
    }

    [Fact]
    public async Task Profile_is_hidden_for_a_deactivated_user()
    {
        await _sut.RegisterAsync(NewRegistration());
        _users.Users.Single().IsActive = false;

        Assert.Null(await _sut.GetProfileAsync(1));
    }
}