using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Insurance.BuildingBlocks.Security;

/// <summary>
/// Every service and the gateway validate the same JWT (issued by the Identity service) with the same settings.
/// The gateway additionally checks that the account is still active; see its Program.cs.
/// </summary>
public static class JwtAuthentication
{
    public static IServiceCollection AddInsuranceJwt(this IServiceCollection services, IConfiguration config,
        Action<JwtBearerEvents>? configureEvents = null)
    {
        var jwt = config.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException(
            "Jwt:Key is missing. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<32+ random chars>\", or the Jwt__Key environment variable.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                if (configureEvents is not null)
                {
                    o.Events = new JwtBearerEvents();
                    configureEvents(o.Events);
                }
            });
        services.AddAuthorization();
        return services;
    }
}
