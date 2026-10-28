using Insurance.Billing.Contracts;
using Insurance.Billing.Repositories;
using Insurance.Billing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Billing;

/// <summary>Premium schedules and payments.</summary>
public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<IPremiumSchedule, PremiumSchedule>();
        return services;
    }
}
