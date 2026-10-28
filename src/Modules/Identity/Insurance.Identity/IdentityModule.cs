using Insurance.Identity.Contracts;
using Insurance.Identity.Repositories;
using Insurance.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.Identity;

/// <summary>Users, sign-in, JWT issuing and customer administration.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();

        services.AddScoped<IUserDirectory, UserDirectory>();
        return services;
    }
}
