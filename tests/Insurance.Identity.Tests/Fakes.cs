using Insurance.Identity.Contracts;
using Insurance.Identity.Exceptions;
using Insurance.Identity.Models;
using Insurance.Identity.Repositories;
using Insurance.Identity.Services;

namespace Insurance.Identity.Tests;

// Fakes are tiny in-memory stand-ins for the 
// real repositories. The services only know the
// interfaces (IUserRepository and so on), so they 
// can't tell the difference. That's dependency injection 
// paying off: no database, no Azure, tests run in milliseconds.

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = new();
    private int _nextId = 1;

    public Task<int> CreateAsync(User user)
    {
        // Mimics what the real repository does when SQL reports a duplicate email.
        if (Users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateEmailException(user.Email);

        user.UserId = _nextId++;
        Users.Add(user);
        return Task.FromResult(user.UserId);
    }

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(int userId) =>
        Task.FromResult(Users.FirstOrDefault(u => u.UserId == userId));

    public Task<List<UserAccount>> GetManyAsync(IEnumerable<int> userIds) =>
        Task.FromResult(Users.Where(u => userIds.Contains(u.UserId))
            .Select(u => new UserAccount(u.UserId, u.FullName, u.Email, u.Role, u.IsActive)).ToList());
}

public class FakeTokenService : ITokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user) =>
        ($"token-for-{user.UserId}", DateTime.UtcNow.AddHours(1));
}
