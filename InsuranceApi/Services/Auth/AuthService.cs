using InsuranceApi.DTOs;
using InsuranceApi.Enums;
using InsuranceApi.Models;
using InsuranceApi.Repositories;

namespace InsuranceApi.Services;

// The service owns BUSINESS rules (hashing, who may register as what, what a login means).
// The repository owns DATA access. The controller owns HTTP.
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),   // salted + slow on purpose
            Phone = request.Phone,
            Address = request.Address,
            Role = UserRole.Customer      // never trust the client to choose its own role
        };

        user.UserId = await _users.CreateAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

        // Same null result for "no such user", "disabled" and "wrong password" so attackers can't probe which emails exist.
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return BuildResponse(user);
    }

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        return user is null || !user.IsActive ? null : MapToDto(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expires) = _tokens.CreateToken(user);
        return new AuthResponse { Token = token, ExpiresAtUtc = expires, User = MapToDto(user) };
    }

    private static UserDto MapToDto(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role.ToString()
    };
}
