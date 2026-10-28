using Insurance.BuildingBlocks.Hosting;
using Insurance.BuildingBlocks.Http;
using Insurance.BuildingBlocks.Messaging;
using Insurance.Catalog.Contracts;
using Insurance.Quotes.Contracts;
using Insurance.Quotes.Messaging;
using Insurance.Quotes.Rating;
using Insurance.Quotes.Repositories;
using Insurance.Quotes.Services;

// Quotes service: the rating engine and personalized quotes.
// Asks Catalog for plans over HTTP; takes part in the enrollment saga (reserve over HTTP, outcome over the bus).
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("Quotes service");

builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<QuoteReservations>();
builder.Services.AddScoped<IQuoteReservations>(sp => sp.GetRequiredService<QuoteReservations>());

builder.Services.AddSingleton<IRatingStrategy, HealthRatingStrategy>();
builder.Services.AddSingleton<IRatingStrategy, LifeRatingStrategy>();
builder.Services.AddSingleton<IRatingStrategy, AutoRatingStrategy>();
builder.Services.AddSingleton<IRatingStrategy, HomeRatingStrategy>();
builder.Services.AddSingleton<IRatingStrategy, TravelRatingStrategy>();

builder.Services.AddServiceClient<IPlanCatalog, PlanCatalogClient>(builder.Configuration, "Catalog");

builder.Services.AddMessaging(builder.Configuration, "quotes", bus =>
{
    bus.AddConsumer<EnrollmentActivatedConsumer>();
    bus.AddConsumer<EnrollmentFailedConsumer>();
});

var app = builder.Build();
await app.UseServiceDefaultsAsync();
app.Run();
