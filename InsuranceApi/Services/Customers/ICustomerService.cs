using InsuranceApi.DTOs;

namespace InsuranceApi.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(string? search, bool? isActive);
    Task<CustomerDto> SetActiveAsync(int userId, bool isActive);   // throws NotFoundException
}