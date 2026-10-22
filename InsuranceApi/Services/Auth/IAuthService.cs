using InsuranceApi.DTOs;

namespace InsuranceApi.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);   // throws DuplicateEmailException
    Task<AuthResponse?> LoginAsync(LoginRequest request);        // null => bad credentials
    Task<UserDto?> GetProfileAsync(int userId);
}
