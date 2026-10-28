using Insurance.BuildingBlocks.Hosting;
using Insurance.Catalog.Contracts;
using Insurance.Catalog.Repositories;
using Insurance.Catalog.Services;

// Catalog service: the plans that can be bought, at what base rate.
// Calls no other service and publishes no events: others ask it over HTTP (/internal/plans).
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("Catalog service");

builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPlanCatalog, PlanCatalog>();

var app = builder.Build();
await app.UseServiceDefaultsAsync();
app.Run();
