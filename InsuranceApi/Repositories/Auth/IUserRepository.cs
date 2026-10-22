using InsuranceApi.Models;

namespace InsuranceApi.Repositories;

public interface IUserRepository
{
    Task<int> CreateAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int userId);
}
