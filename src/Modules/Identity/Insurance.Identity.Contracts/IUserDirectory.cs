using Insurance.BuildingBlocks.Security;

namespace Insurance.Identity.Contracts;

/// <summary>Read access to user accounts for other modules and the host. Never exposes the password hash.</summary>
public interface IUserDirectory
{
    Task<UserAccount?> GetAsync(int userId);   // null => no such user
}

public record UserAccount(int UserId, string FullName, string Email, UserRole Role, bool IsActive);
