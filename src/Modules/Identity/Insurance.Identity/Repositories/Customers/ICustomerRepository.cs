using Insurance.Identity.Models;

namespace Insurance.Identity.Repositories;

public interface ICustomerRepository
{
    Task<List<CustomerSummary>> GetAllAsync(string? search, bool? isActive);
    Task<CustomerSummary?> SetActiveAsync(int userId, bool isActive);   // null => not a customer / not found
}