using Insurance.BuildingBlocks.Exceptions;
using Insurance.Identity.Contracts;
using Insurance.Identity.Repositories;

namespace Insurance.Identity.Services;

// Policy and claim counts belong to other services; the gateway's BFF adds them to the admin screen.
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    public CustomerService(ICustomerRepository customers) => _customers = customers;

    public Task<List<CustomerInfo>> GetAllAsync(string? search, bool? isActive)
    {
        // Treat blank search as "no search".
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return _customers.GetAllAsync(term, isActive);
    }

    public async Task<CustomerInfo> SetActiveAsync(int userId, bool isActive) =>
        await _customers.SetActiveAsync(userId, isActive)
            ?? throw new NotFoundException("Customer not found.");   // ApiExceptionHandler turns this into a 404
}
