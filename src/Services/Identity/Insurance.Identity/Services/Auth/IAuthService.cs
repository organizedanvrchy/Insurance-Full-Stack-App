using Insurance.Identity.DTOs;

namespace Insurance.Identity.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);   // throws DuplicateEmailException
    Task<AuthResponse?> LoginAsync(LoginRequest request);        // null => bad credentials
    Task<UserDto?> GetProfileAsync(int userId);
}
