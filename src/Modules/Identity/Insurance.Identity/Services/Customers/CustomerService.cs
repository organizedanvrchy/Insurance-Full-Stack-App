using Insurance.BuildingBlocks.Exceptions;
using Insurance.Claims.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Identity.DTOs;
using Insurance.Identity.Models;
using Insurance.Identity.Repositories;

namespace Insurance.Identity.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IEnrollmentLookup _enrollments;
    private readonly IClaimStatistics _claims;

    public CustomerService(ICustomerRepository customers, IEnrollmentLookup enrollments, IClaimStatistics claims)
    {
        _customers = customers;
        _enrollments = enrollments;
        _claims = claims;
    }

    public async Task<List<CustomerDto>> GetAllAsync(string? search, bool? isActive)
    {
        // Treat blank search as "no search".
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var list = await _customers.GetAllAsync(term, isActive);
        await AddCountsAsync(list);
        return list.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto> SetActiveAsync(int userId, bool isActive)
    {
        var updated = await _customers.SetActiveAsync(userId, isActive)
            ?? throw new NotFoundException("Customer not found.");   // ApiExceptionHandler turns this into a 404
        await AddCountsAsync(new[] { updated });
        return MapToDto(updated);
    }

    // Policies and claims belong to other modules, so their counts are fetched in one call each.
    private async Task AddCountsAsync(IReadOnlyCollection<CustomerSummary> customers)
    {
        if (customers.Count == 0) return;

        var ids = customers.Select(c => c.UserId).ToList();
        var policies = await _enrollments.CountActiveByUserAsync(ids);
        var claims = await _claims.GetCountsByUserAsync(ids);

        foreach (var c in customers)
        {
            c.ActivePolicies = policies.GetValueOrDefault(c.UserId);
            if (claims.TryGetValue(c.UserId, out var counts))
            {
                c.TotalClaims = counts.TotalClaims;
                c.OpenClaims = counts.OpenClaims;
            }
        }
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