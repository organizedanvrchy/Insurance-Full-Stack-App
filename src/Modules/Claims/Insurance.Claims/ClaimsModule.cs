using Insurance.Claims.Contracts;
using Insurance.Claims.Repositories;
using Insurance.Claims.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Claims;

/// <summary>Filing claims and the admin review workflow.</summary>
public static class ClaimsModule
{
    public static IServiceCollection AddClaimsModule(this IServiceCollection services)
    {
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IClaimService, ClaimService>();

        services.AddScoped<IClaimStatistics, ClaimStatistics>();
        return services;
    }
}
