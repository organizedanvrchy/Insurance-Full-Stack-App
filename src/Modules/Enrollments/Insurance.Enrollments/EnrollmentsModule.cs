using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Repositories;
using Insurance.Enrollments.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Enrollments;

/// <summary>Policies a customer holds: enrolling from a quote, viewing and cancelling.</summary>
public static class EnrollmentsModule
{
    public static IServiceCollection AddEnrollmentsModule(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        services.AddScoped<IEnrollmentLookup, EnrollmentLookup>();
        return services;
    }
}
