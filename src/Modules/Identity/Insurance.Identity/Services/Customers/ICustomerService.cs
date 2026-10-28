using Insurance.Identity.DTOs;

namespace Insurance.Identity.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(string? search, bool? isActive);
    Task<CustomerDto> SetActiveAsync(int userId, bool isActive);   // throws NotFoundException
}