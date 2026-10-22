using InsuranceApi.DTOs;
using InsuranceApi.Exceptions;
using InsuranceApi.Models;
using InsuranceApi.Repositories;

namespace InsuranceApi.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    public CustomerService(ICustomerRepository customers) => _customers = customers;

    public async Task<List<CustomerDto>> GetAllAsync(string? search, bool? isActive)
    {
        // Treat blank search as "no search".
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var list = await _customers.GetAllAsync(term, isActive);
        return list.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto> SetActiveAsync(int userId, bool isActive)
    {
        var updated = await _customers.SetActiveAsync(userId, isActive)
            ?? throw new NotFoundException("Customer not found.");   // ApiExceptionHandler turns this into a 404
        return MapToDto(updated);
    }

    private static CustomerDto MapToDto(CustomerSummary c) => new()
    {
        UserId = c.UserId,
        FullName = c.FullName,
        Email = c.Email,
        Phone = c.Phone,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        ActivePolicies = c.ActivePolicies,
        TotalClaims = c.TotalClaims,
        OpenClaims = c.OpenClaims
    };
}