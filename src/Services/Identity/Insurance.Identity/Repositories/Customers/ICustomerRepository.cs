using Insurance.Identity.Contracts;

namespace Insurance.Identity.Repositories;

public interface ICustomerRepository
{
    Task<List<CustomerInfo>> GetAllAsync(string? search, bool? isActive);
    Task<CustomerInfo?> SetActiveAsync(int userId, bool isActive);   // null => not a customer / not found
}