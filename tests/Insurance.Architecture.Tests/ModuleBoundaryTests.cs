using System.Reflection;
using Insurance.Billing;
using Insurance.Catalog;
using Insurance.Claims;
using Insurance.Enrollments;
using Insurance.Identity;
using Insurance.Quotes;
using Xunit;

namespace Insurance.Architecture.Tests;

// These tests keep the modular monolith honest: each module must stay extractable into its own service.
// A module may use BuildingBlocks and other modules' *.Contracts, never another module's implementation.

public class ModuleBoundaryTests
{
    private const string BuildingBlocks = "Insurance.BuildingBlocks";

    public static TheoryData<string> Modules => new()
    {
        typeof(IdentityModule).Assembly.GetName().Name!,
        typeof(CatalogModule).Assembly.GetName().Name!,
        typeof(QuotesModule).Assembly.GetName().Name!,
        typeof(EnrollmentsModule).Assembly.GetName().Name!,
        typeof(BillingModule).Assembly.GetName().Name!,
        typeof(ClaimsModule).Assembly.GetName().Name!
    };

    private static IEnumerable<string> InsuranceReferences(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("Insurance."));

    [Theory]
    [MemberData(nameof(Modules))]
    public void Modules_reference_only_building_blocks_and_contracts(string module)
    {
        var illegal = InsuranceReferences(module)
            .Where(n => n != BuildingBlocks && !n.EndsWith(".Contracts"))
            .ToList();

        Assert.True(illegal.Count == 0, $"{module} references another module's implementation: {string.Join(", ", illegal)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Contracts_reference_only_building_blocks_and_other_contracts(string module)
    {
        var contracts = module + ".Contracts";
        var illegal = InsuranceReferences(contracts)
            .Where(n => n != BuildingBlocks && !n.EndsWith(".Contracts"))
            .ToList();

        Assert.True(illegal.Count == 0, $"{contracts} references an implementation: {string.Join(", ", illegal)}");
    }
}
