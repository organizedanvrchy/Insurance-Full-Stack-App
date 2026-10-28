using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Insurance.BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Insurance.BuildingBlocks.Http;

/// <summary>
/// Service-to-service HTTP. Each *.Contracts project ships a typed client for its service's internal endpoints;
/// a caller registers it with <see cref="AddServiceClient{TClient, TImplementation}"/>, which reads the address
/// from "Services:&lt;name&gt;" and forwards the caller's JWT, so the called service sees the same user.
/// </summary>
public static class ServiceClients
{
    /// <summary>The same wire format the services use: camelCase, enums as names.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IServiceCollection AddServiceClientDefaults(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthorizationHandler>();
        return services;
    }

    public static IHttpClientBuilder AddServiceClient<TClient, TImplementation>(
        this IServiceCollection services, IConfiguration config, string serviceName)
        where TClient : class
        where TImplementation : class, TClient
    {
        var address = config[$"Services:{serviceName}"]
            ?? throw new InvalidOperationException($"Services:{serviceName} (the {serviceName} service's address) is missing.");

        services.AddServiceClientDefaults();
        return services.AddHttpClient<TClient, TImplementation>(c => c.BaseAddress = new Uri(address))
            .AddHttpMessageHandler<ForwardAuthorizationHandler>();
    }

    /// <summary>Reads the body, or turns the other service's ProblemDetails back into the same typed exception.</summary>
    public static async Task<T> ReadOrThrowAsync<T>(this HttpResponseMessage response)
    {
        await response.ThrowIfFailedAsync();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static async Task ThrowIfFailedAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var title = await ReadTitleAsync(response);
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new BusinessRuleException(title),
            HttpStatusCode.NotFound   => new NotFoundException(title),
            HttpStatusCode.Conflict   => new ConflictException(title),
            _ => new HttpRequestException(
                $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} returned {(int)response.StatusCode}: {title}",
                null, response.StatusCode)
        };
    }

    private static async Task<string> ReadTitleAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
            if (!string.IsNullOrEmpty(problem?.Title)) return problem.Title;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { }
        return response.ReasonPhrase ?? response.StatusCode.ToString();
    }

    /// <summary>"?ids=1&amp;ids=2" for a list parameter.</summary>
    public static string IdQuery(string name, IEnumerable<int> ids) =>
        string.Join("&", ids.Distinct().Select(id => $"{name}={id}"));
}

/// <summary>Copies the incoming request's Authorization header onto outgoing service calls.</summary>
public class ForwardAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _context;
    public ForwardAuthorizationHandler(IHttpContextAccessor context) => _context = context;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var auth = _context.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(auth))
            request.Headers.TryAddWithoutValidation("Authorization", auth);
        return base.SendAsync(request, ct);
    }
}
