using Insurance.Catalog.Contracts;
using Insurance.Catalog.Repositories;
using Insurance.Catalog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Catalog;

/// <summary>The plan catalog: what can be bought, at what base rate.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<IPolicyRepository, PolicyRepository>();

        services.AddScoped<IPlanCatalog, PlanCatalog>();
        return services;
    }
}
