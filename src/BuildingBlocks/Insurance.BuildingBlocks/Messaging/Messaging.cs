using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.BuildingBlocks.Messaging;

/// <summary>
/// The event bus: MassTransit on RabbitMQ. Each service gets its own queues (prefixed with its name), so every
/// service that subscribes to an event receives its own copy. Failed deliveries are retried; after the last retry
/// MassTransit publishes a Fault&lt;T&gt;, which is how a saga hears that a step failed for good.
/// Swapping to Azure Service Bus is a change here (UsingAzureServiceBus), not in the services.
/// </summary>
public static class Messaging
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration config,
        string serviceName, Action<IBusRegistrationConfigurator>? addConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, includeNamespace: false));
            addConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = config.GetSection("RabbitMq");
                cfg.Host(rabbit["Host"] ?? "localhost", rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"] ?? "guest");
                    h.Password(rabbit["Password"] ?? "guest");
                });

                cfg.UseMessageRetry(r => r.Intervals(100, 500, 1_000, 5_000, 15_000));
                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }
}
