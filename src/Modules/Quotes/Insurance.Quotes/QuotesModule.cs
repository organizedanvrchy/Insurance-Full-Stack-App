using Insurance.Quotes.Rating;
using Insurance.Quotes.Repositories;
using Insurance.Quotes.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Quotes;

/// <summary>Rating engine and personalized quotes.</summary>
public static class QuotesModule
{
    public static IServiceCollection AddQuotesModule(this IServiceCollection services)
    {
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IQuoteService, QuoteService>();

        services.AddSingleton<IRatingStrategy, HealthRatingStrategy>();
        services.AddSingleton<IRatingStrategy, LifeRatingStrategy>();
        services.AddSingleton<IRatingStrategy, AutoRatingStrategy>();
        services.AddSingleton<IRatingStrategy, HomeRatingStrategy>();
        services.AddSingleton<IRatingStrategy, TravelRatingStrategy>();
        return services;
    }
}
