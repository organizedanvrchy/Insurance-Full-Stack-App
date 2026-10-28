using Insurance.Identity.Contracts;

namespace Insurance.Identity.Services;

public interface ICustomerService
{
    Task<List<CustomerInfo>> GetAllAsync(string? search, bool? isActive);
    Task<CustomerInfo> SetActiveAsync(int userId, bool isActive);   // throws NotFoundException
}
